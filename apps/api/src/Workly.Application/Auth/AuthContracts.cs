using System.ComponentModel.DataAnnotations;

namespace Workly.Application.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128, MinimumLength = 15)] string Password,
    [Required, StringLength(120)] string DisplayName);

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128)] string Password);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);
public sealed record AuthResponse(
    string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserResponse User);
public sealed record AuthSession(
    AuthResponse Response, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

public interface IAuthService
{
    Task<AuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<UserResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class AuthException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
