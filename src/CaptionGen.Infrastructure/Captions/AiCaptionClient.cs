using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaptionGen.Application.Captions;
using Microsoft.Extensions.Logging;

namespace CaptionGen.Infrastructure.Captions;

public sealed class AiCaptionClient : IAiCaptionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiCaptionClient> _logger;

    public AiCaptionClient(HttpClient httpClient, ILogger<AiCaptionClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CaptionGenerationResult> GenerateAsync(
        string description,
        string platform,
        string tone,
        int count,
        CaptionGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        var payload = new GenerateCaptionRequest(
            description,
            platform,
            tone,
            count,
            options.Language,
            options.Audience,
            options.Goal,
            options.CaptionLength,
            options.IncludeEmojis,
            options.IncludeCta,
            options.HashtagCount,
            options.BrandVoice,
            options.ForbiddenWords?.ToArray() ?? Array.Empty<string>(),
            options.KeywordsToInclude?.ToArray() ?? Array.Empty<string>());

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("generate-caption", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new AiServiceException("Failed to reach the AI caption service.", null, ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;
            var message = ExtractMessage(body) ?? $"AI service error ({status}).";
            _logger.LogWarning("AI service responded with {Status}: {Body}", status, body);
            throw new AiServiceException(message, status);
        }

        GenerateCaptionResponse? data;
        try
        {
            data = await response.Content.ReadFromJsonAsync<GenerateCaptionResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiServiceException($"AI service returned unreadable JSON: {ex.Message}. Body: {body}", (int)response.StatusCode, ex);
        }

        if (data is null || data.Captions is null || data.Captions.Count == 0)
        {
            throw new AiServiceException("AI service returned an empty response.", (int)response.StatusCode);
        }

        var bestIdx = data.BestCaptionIndex >= 0 && data.BestCaptionIndex < data.Captions.Count
            ? data.BestCaptionIndex
            : 0;

        var bestHashtags = data.Captions[bestIdx].Hashtags ?? new List<string>();
        if (bestHashtags.Count == 0)
        {
            bestHashtags = data.Captions.FirstOrDefault(c => c.Hashtags is { Count: > 0 })?.Hashtags ?? new List<string>();
        }

        if (bestHashtags.Count == 0)
        {
            throw new AiServiceException("AI service returned no hashtags.", (int)response.StatusCode);
        }

        var captionTexts = data.Captions.Select(c => c.Text).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (captionTexts.Count == 0)
        {
            throw new AiServiceException("AI service returned empty caption texts.", (int)response.StatusCode);
        }

        return new CaptionGenerationResult(captionTexts, bestHashtags);
    }

    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString();
            }
        }
        catch
        {
            // ignored
        }

        return body;
    }

    private sealed record GenerateCaptionRequest(
        string Description,
        string Platform,
        string Tone,
        int Count,
        string Language,
        string? Audience,
        string Goal,
        [property: JsonPropertyName("caption_length")] string CaptionLength,
        [property: JsonPropertyName("include_emojis")] bool IncludeEmojis,
        [property: JsonPropertyName("include_cta")] bool IncludeCta,
        [property: JsonPropertyName("hashtag_count")] int HashtagCount,
        [property: JsonPropertyName("brand_voice")] string? BrandVoice,
        [property: JsonPropertyName("forbidden_words")] string[] ForbiddenWords,
        [property: JsonPropertyName("keywords_to_include")] string[] KeywordsToInclude);

    private sealed record GenerateCaptionResponse(
        List<CaptionDto> Captions,
        [property: JsonPropertyName("best_caption_index")] int BestCaptionIndex);

    private sealed record CaptionDto(string Text, List<string>? Hashtags);
}
