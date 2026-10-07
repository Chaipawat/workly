using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Workly.Application.Auth;

namespace Workly.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IAuthService auth, IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookieName = "workly_refresh";

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var session = await auth.RegisterAsync(request, cancellationToken);
        WriteRefreshCookie(session);
        return StatusCode(StatusCodes.Status201Created, session.Response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await auth.LoginAsync(request, cancellationToken);
        WriteRefreshCookie(session);
        return session.Response;
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = ReadRefreshCookie();
        var session = await auth.RefreshAsync(refreshToken, cancellationToken);
        WriteRefreshCookie(session);
        return session.Response;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
            await auth.LogoutAsync(refreshToken, cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, CookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            return Unauthorized();
        return await auth.GetUserAsync(userId, cancellationToken);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserResponse>> UpdateProfile(UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            return Unauthorized();
        return await auth.UpdateProfileAsync(userId, request, cancellationToken);
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            return Unauthorized();
        await auth.ChangePasswordAsync(userId, request, cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, CookieOptions());
        return NoContent();
    }

    private string ReadRefreshCookie()
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
            return refreshToken;
        throw new AuthException(StatusCodes.Status401Unauthorized, "Missing refresh session.");
    }

    private void WriteRefreshCookie(AuthSession session)
    {
        var options = CookieOptions();
        options.Expires = session.RefreshTokenExpiresAt;
        Response.Cookies.Append(RefreshCookieName, session.RefreshToken, options);
    }

    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth"
    };
}
