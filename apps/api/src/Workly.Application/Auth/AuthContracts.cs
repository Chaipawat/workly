using System.ComponentModel.DataAnnotations;

namespace Workly.Application.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128, MinimumLength = 6)] string Password,
    [Required, StringLength(120)] string DisplayName);

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128)] string Password);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);
public sealed record AuthResponse(
    string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserResponse User);
public sealed record AuthSession(
    AuthResponse Response, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

public sealed class ChangePasswordRequest
{
    [Required, StringLength(128)] public required string CurrentPassword { get; init; }
    [Required, StringLength(128, MinimumLength = 6)] public required string NewPassword { get; init; }
}

public sealed class UpdateProfileRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public required string DisplayName { get; init; }
}

public interface IAuthService
{
    Task<AuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<UserResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}

public sealed class AuthException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
