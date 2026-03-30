namespace CaptionGen.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessMinutes { get; init; } = 60;
    public string CookieName { get; init; } = "auth_token";
    public bool AllowInsecureCookieOnHttp { get; init; }
}
