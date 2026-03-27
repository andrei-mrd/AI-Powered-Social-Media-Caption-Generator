using System.Diagnostics;
using System.Net.Http;
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
        var traceId = CreateTraceId();
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
            options.KeywordsToInclude?.ToArray() ?? Array.Empty<string>(),
            options.MediaUrls?.ToArray() ?? Array.Empty<string>());

        HttpResponseMessage response;
        try
        {
            using var request = BuildPostRequest("generate-caption", payload, traceId);
            response = await _httpClient.SendAsync(request, cancellationToken);
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

        var variants = data.Captions
            .Select((c, idx) => new GeneratedCaption(
                c.Text,
                c.Hashtags ?? new List<string>(),
                string.IsNullOrWhiteSpace(c.Hook) ? null : c.Hook,
                string.IsNullOrWhiteSpace(c.Cta) ? null : c.Cta,
                c.Score,
                c.ScoreReason))
            .Where(v => !string.IsNullOrWhiteSpace(v.Text))
            .ToList();

        if (variants.Count == 0)
        {
            throw new AiServiceException("AI service returned empty caption texts.", (int)response.StatusCode);
        }

        var engagementScore = data.Metadata?.EngagementScore;
        var engagementReason = data.Metadata?.EngagementRationale;
        var outgoingTrace = data.TraceId ?? data.Metadata?.TraceId ?? traceId;

        return new CaptionGenerationResult(variants, bestHashtags, engagementScore, engagementReason, outgoingTrace);
    }

    public async Task<CaptionImprovementResult> ImproveAsync(
        string caption,
        string platform,
        string tone,
        string language,
        string goal,
        CancellationToken cancellationToken = default)
    {
        var traceId = CreateTraceId();
        var payload = new ImproveCaptionRequest(caption, platform, tone, language, goal);

        HttpResponseMessage response;
        try
        {
            using var request = BuildPostRequest("improve-caption", payload, traceId);
            response = await _httpClient.SendAsync(request, cancellationToken);
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

        ImproveCaptionResponse? data;
        try
        {
            data = await response.Content.ReadFromJsonAsync<ImproveCaptionResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiServiceException($"AI service returned unreadable JSON: {ex.Message}. Body: {body}", (int)response.StatusCode, ex);
        }

        if (data is null || string.IsNullOrWhiteSpace(data.ImprovedCaption))
        {
            throw new AiServiceException("AI service returned an empty improved caption.", (int)response.StatusCode);
        }

        var outgoingTrace = data.TraceId ?? traceId;
        return new CaptionImprovementResult(data.ImprovedCaption, data.ShorterVersion, data.StrongerCtaVersion, outgoingTrace);
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

    private static string CreateTraceId() =>
        Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

    private static HttpRequestMessage BuildPostRequest<T>(string path, T payload, string traceId)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        message.Headers.Remove("X-Request-ID");
        message.Headers.Add("X-Request-ID", traceId);
        return message;
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
        [property: JsonPropertyName("keywords_to_include")] string[] KeywordsToInclude,
        [property: JsonPropertyName("media_urls")] string[] MediaUrls);

    private sealed record GenerateCaptionResponse(
        List<CaptionDto> Captions,
        [property: JsonPropertyName("best_caption_index")] int BestCaptionIndex,
        CaptionMetadataDto? Metadata,
        [property: JsonPropertyName("trace_id")] string? TraceId);

    private sealed record CaptionDto(
        string Text,
        List<string>? Hashtags,
        string? Hook,
        string? Cta,
        int? Score,
        [property: JsonPropertyName("score_reason")] string? ScoreReason);

    private sealed record CaptionMetadataDto(
        [property: JsonPropertyName("engagement_score")] int? EngagementScore,
        [property: JsonPropertyName("engagement_rationale")] string? EngagementRationale,
        [property: JsonPropertyName("trace_id")] string? TraceId,
        [property: JsonPropertyName("media_urls")] List<string>? MediaUrls,
        [property: JsonPropertyName("media_cues")] string? MediaCues);

    private sealed record ImproveCaptionRequest(
        string Caption,
        string Platform,
        string Tone,
        string Language,
        string Goal);

    private sealed record ImproveCaptionResponse(
        [property: JsonPropertyName("improved_caption")] string ImprovedCaption,
        [property: JsonPropertyName("shorter_version")] string ShorterVersion,
        [property: JsonPropertyName("stronger_cta_version")] string StrongerCtaVersion,
        [property: JsonPropertyName("trace_id")] string? TraceId);
}
