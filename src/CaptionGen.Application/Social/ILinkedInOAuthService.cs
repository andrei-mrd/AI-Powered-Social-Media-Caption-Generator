namespace CaptionGen.Application.Social;

public sealed record LinkedInTokens(
    string AccessToken,
    string? RefreshToken,
    int ExpiresInSeconds,
    int RefreshTokenExpiresInSeconds);

public sealed record LinkedInProfile(
    string Sub,
    string Name);

public interface ILinkedInOAuthService
{
    string BuildAuthorizationUrl(string state);
    Task<LinkedInTokens> ExchangeCodeAsync(string code, CancellationToken ct);
    Task<LinkedInProfile> GetProfileAsync(string accessToken, CancellationToken ct);
}
