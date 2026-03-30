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

public sealed record CaptionImprovementResult(
    string ImprovedCaption,
    string ShorterVersion,
    string StrongerCtaVersion,
    string? TraceId = null);
