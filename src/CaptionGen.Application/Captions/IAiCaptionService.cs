using System.Collections.Generic;

namespace CaptionGen.Application.Captions;

public sealed record CaptionGenerationResult(
    IReadOnlyList<GeneratedCaption> Captions,
    IReadOnlyList<string> Hashtags,
    int? EngagementScore = null,
    string? EngagementRationale = null,
    string? TraceId = null);

public sealed record GeneratedCaption(
    string Text,
    IReadOnlyList<string> Hashtags,
    string? Hook,
    string? Cta,
    int? Score,
    string? ScoreReason = null);

public sealed record CaptionGenerationOptions(
    string Language,
    string Goal,
    string CaptionLength,
    bool IncludeEmojis,
    bool IncludeCta,
    int HashtagCount,
    string? Audience,
    string? BrandVoice,
    IReadOnlyList<string> ForbiddenWords,
    IReadOnlyList<string> KeywordsToInclude,
    IReadOnlyList<string> MediaUrls);

public interface IAiCaptionService
{
    Task<CaptionGenerationResult> GenerateAsync(
        string description,
        string platform,
        string tone,
        int count,
        CaptionGenerationOptions options,
        CancellationToken cancellationToken = default);

    Task<CaptionImprovementResult> ImproveAsync(
        string caption,
        string platform,
        string tone,
        string language,
        string goal,
        CancellationToken cancellationToken = default);
}

public sealed class AiServiceException : Exception
{
    public int? StatusCode { get; }

    public bool IsClientError => StatusCode is >= 400 and < 500;

    public AiServiceException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

public sealed record CaptionImprovementResult(
    string ImprovedCaption,
    string ShorterVersion,
    string StrongerCtaVersion,
    string? TraceId = null);
