using System.ComponentModel.DataAnnotations;

namespace Workly.Application.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128, MinimumLength = 15)] string Password,
    [Required, StringLength(120)] string DisplayName);

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128)] string Password);

public sealed record RefreshRequest(
    [Required, StringLength(128)] string RefreshToken);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);
public sealed record AuthResponse(
    string AccessToken, DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, UserResponse User);

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<UserResponse> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class AuthException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
