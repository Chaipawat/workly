using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Workly.Application.Auth;
using Workly.Domain.Entities;
using Workly.Infrastructure.Persistence;

namespace Workly.Infrastructure.Auth;

public sealed class AuthService(WorklyDbContext db, JwtSettings settings) : IAuthService
{
    private static readonly PasswordHasher<User> PasswordHasher = new();
    private static readonly User DummyUser = new()
    {
        Email = "dummy@example.invalid", PasswordHash = "", DisplayName = "Dummy"
    };
    private static readonly string DummyHash = PasswordHasher.HashPassword(DummyUser, "dummy-password-for-timing");

    public async Task<AuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new AuthException(409, "Email is already registered.");

        var user = new User { Email = email, DisplayName = request.DisplayName.Trim(), PasswordHash = "" };
        user.PasswordHash = PasswordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        var response = CreateSession(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AuthException(409, "Email is already registered.");
        }
        return response;
    }

    public async Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        var result = PasswordHasher.VerifyHashedPassword(user ?? DummyUser,
            user?.PasswordHash ?? DummyHash, request.Password);
        if (user is null || result == PasswordVerificationResult.Failed)
            throw new AuthException(401, "Invalid email or password.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = PasswordHasher.HashPassword(user, request.Password);
        var response = CreateSession(user);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = HashToken(refreshToken);
        var userId = await db.RefreshTokens.Where(x => x.TokenHash == hash)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken);
        if (userId is null) throw InvalidRefresh();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize session changes per user. A second request sees the committed revocation.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM users WHERE id = {userId.Value} FOR UPDATE", cancellationToken);
        var token = await db.RefreshTokens.Include(x => x.User)
            .SingleAsync(x => x.TokenHash == hash, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (token.RevokedAt is not null)
        {
            if (token.ReplacedById is not null)
            {
                await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAt, now), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            throw InvalidRefresh();
        }
        if (token.ExpiresAt <= now) throw InvalidRefresh();

        var response = CreateSession(token.User, token.ExpiresAt);
        var replacement = db.ChangeTracker.Entries<RefreshToken>()
            .Single(x => x.State == EntityState.Added).Entity;
        token.RevokedAt = now;
        token.ReplacedBy = replacement;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = HashToken(refreshToken);
        var userId = await db.RefreshTokens.Where(x => x.TokenHash == hash)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken);
        if (userId is null) return;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM users WHERE id = {userId.Value} FOR UPDATE", cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var token = await db.RefreshTokens.SingleAsync(x => x.TokenHash == hash, cancellationToken);
        // Revoke this session and its replacements, including an in-flight rotation.
        while (true)
        {
            token.RevokedAt ??= now;
            if (token.ReplacedById is not Guid nextId) break;
            token = await db.RefreshTokens.SingleAsync(x => x.Id == nextId, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<UserResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Users.Where(x => x.Id == userId)
            .Select(x => new UserResponse(x.Id, x.Email, x.DisplayName))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AuthException(401, "User no longer exists.");
    }

    private AuthSession CreateSession(User user, DateTimeOffset? refreshExpiresAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExpiresAt = now.AddMinutes(settings.AccessTokenMinutes);
        var refreshExpiry = refreshExpiresAt ?? now.AddDays(settings.RefreshTokenDays);
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id, User = user, TokenHash = HashToken(rawToken), ExpiresAt = refreshExpiry
        });
        var jwt = new JwtSecurityToken(settings.Issuer, settings.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())],
            now.UtcDateTime, accessExpiresAt.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        var response = new AuthResponse(new JwtSecurityTokenHandler().WriteToken(jwt), accessExpiresAt,
            new UserResponse(user.Id, user.Email, user.DisplayName));
        return new AuthSession(response, rawToken, refreshExpiry);
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static AuthException InvalidRefresh() => new(401, "Invalid or expired refresh token.");
}
