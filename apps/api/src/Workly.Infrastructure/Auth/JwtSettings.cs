namespace Workly.Infrastructure.Auth;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "Workly.Api";
    public string Audience { get; init; } = "Workly.Web";
    public required string SigningKey { get; init; }
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
