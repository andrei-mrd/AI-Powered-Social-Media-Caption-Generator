using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CaptionGen.Application.Social;
using Microsoft.Extensions.Options;

namespace CaptionGen.Infrastructure.Social;

internal sealed class LinkedInTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("refresh_token_expires_in")]
    public int RefreshTokenExpiresIn { get; init; }
}

internal sealed class LinkedInProfileResponse
{
    [JsonPropertyName("sub")]
    public string Sub { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed class LinkedInOAuthService : ILinkedInOAuthService
{
    private readonly HttpClient _http;
    private readonly LinkedInOptions _options;

    public LinkedInOAuthService(HttpClient http, IOptions<LinkedInOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public string BuildAuthorizationUrl(string state)
    {
        return "https://www.linkedin.com/oauth/v2/authorization" +
               $"?response_type=code" +
               $"&client_id={Uri.EscapeDataString(_options.ClientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope=openid%20profile%20w_member_social";
    }

    public async Task<LinkedInTokens> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        });

        var response = await _http.PostAsync("https://www.linkedin.com/oauth/v2/accessToken", form, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LinkedInTokenResponse>(ct)
            ?? throw new InvalidOperationException("LinkedIn returned an empty token response.");

        return new LinkedInTokens(
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresIn,
            result.RefreshTokenExpiresIn);
    }

    public async Task<LinkedInProfile> GetProfileAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LinkedInProfileResponse>(ct)
            ?? throw new InvalidOperationException("LinkedIn returned an empty profile response.");

        return new LinkedInProfile(result.Sub, result.Name);
    }
}
