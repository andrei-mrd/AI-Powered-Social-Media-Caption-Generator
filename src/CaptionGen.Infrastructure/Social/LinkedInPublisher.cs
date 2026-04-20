using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CaptionGen.Application.Social;
using CaptionGen.Domain.Social;

namespace CaptionGen.Infrastructure.Social;

public sealed class LinkedInPublisher : ISocialPublisher
{
    private readonly HttpClient _http;
    private readonly ITokenEncryptionService _encryption;

    public string Platform => "linkedin";

    public LinkedInPublisher(HttpClient http, ITokenEncryptionService encryption)
    {
        _http = http;
        _encryption = encryption;
    }

    public async Task PublishAsync(SocialAccount account, string text, CancellationToken ct)
    {
        var accessToken = _encryption.Decrypt(account.AccessTokenEncrypted);
        var authorUrn = $"urn:li:person:{account.PlatformUserId}";

        var payload = new JsonObject
        {
            ["author"] = authorUrn,
            ["lifecycleState"] = "PUBLISHED",
            ["specificContent"] = new JsonObject
            {
                ["com.linkedin.ugc.ShareContent"] = new JsonObject
                {
                    ["shareCommentary"] = new JsonObject { ["text"] = text },
                    ["shareMediaCategory"] = "NONE"
                }
            },
            ["visibility"] = new JsonObject
            {
                ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"LinkedIn API error {(int)response.StatusCode}: {body}");
        }
    }
}
