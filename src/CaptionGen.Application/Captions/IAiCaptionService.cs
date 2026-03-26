using System.Collections.Generic;

namespace CaptionGen.Application.Captions;

public sealed record CaptionGenerationResult(
    IReadOnlyList<string> Captions,
    IReadOnlyList<string> Hashtags);

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
    IReadOnlyList<string> KeywordsToInclude);

public interface IAiCaptionService
{
    Task<CaptionGenerationResult> GenerateAsync(
        string description,
        string platform,
        string tone,
        int count,
        CaptionGenerationOptions options,
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
