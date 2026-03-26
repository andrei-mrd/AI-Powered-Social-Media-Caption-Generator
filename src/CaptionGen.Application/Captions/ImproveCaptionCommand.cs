using MediatR;

namespace CaptionGen.Application.Captions;

public sealed record ImproveCaptionCommand(
    string Caption,
    string Platform,
    string Tone,
    string Language,
    string Goal) : IRequest<CaptionImprovementResult>;

public sealed class ImproveCaptionHandler : IRequestHandler<ImproveCaptionCommand, CaptionImprovementResult>
{
    private readonly IAiCaptionService _ai;

    private static readonly HashSet<string> AllowedPlatforms =
        new(StringComparer.OrdinalIgnoreCase) { "instagram", "tiktok", "linkedin" };

    private static readonly HashSet<string> AllowedTones =
        new(StringComparer.OrdinalIgnoreCase) { "funny", "professional", "inspirational" };

    private static readonly HashSet<string> AllowedGoals =
        new(StringComparer.OrdinalIgnoreCase) { "engagement", "sales", "awareness" };

    public ImproveCaptionHandler(IAiCaptionService ai)
    {
        _ai = ai;
    }

    public Task<CaptionImprovementResult> Handle(ImproveCaptionCommand request, CancellationToken cancellationToken)
    {
        var caption = (request.Caption ?? string.Empty).Trim();
        var platform = (request.Platform ?? string.Empty).Trim().ToLowerInvariant();
        var tone = (request.Tone ?? string.Empty).Trim().ToLowerInvariant();
        var language = (request.Language ?? string.Empty).Trim().ToLowerInvariant();
        var goal = (request.Goal ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(caption))
            throw new InvalidOperationException("Caption is required.");

        if (!AllowedPlatforms.Contains(platform))
            throw new InvalidOperationException("Platform must be instagram, tiktok, or linkedin.");

        if (!AllowedTones.Contains(tone))
            throw new InvalidOperationException("Tone must be funny, professional, or inspirational.");

        if (string.IsNullOrWhiteSpace(language))
            throw new InvalidOperationException("Language is required.");

        if (!AllowedGoals.Contains(goal))
            throw new InvalidOperationException("Goal must be engagement, sales, or awareness.");

        return _ai.ImproveAsync(caption, platform, tone, language, goal, cancellationToken);
    }
}
