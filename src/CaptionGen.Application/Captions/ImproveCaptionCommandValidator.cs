using FluentValidation;

namespace CaptionGen.Application.Captions;

public sealed class ImproveCaptionCommandValidator : AbstractValidator<ImproveCaptionCommand>
{
    private static readonly HashSet<string> AllowedPlatforms =
        new(StringComparer.OrdinalIgnoreCase) { "instagram", "tiktok", "linkedin" };

    private static readonly HashSet<string> AllowedTones =
        new(StringComparer.OrdinalIgnoreCase) { "funny", "professional", "inspirational" };

    private static readonly HashSet<string> AllowedGoals =
        new(StringComparer.OrdinalIgnoreCase) { "engagement", "sales", "awareness" };

    public ImproveCaptionCommandValidator()
    {
        RuleFor(x => x.Caption).NotEmpty();
        RuleFor(x => x.Platform)
            .NotEmpty()
            .Must(p => AllowedPlatforms.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("Platform must be instagram, tiktok, or linkedin.");
        RuleFor(x => x.Tone)
            .NotEmpty()
            .Must(t => AllowedTones.Contains(t.Trim().ToLowerInvariant()))
            .WithMessage("Tone must be funny, professional, or inspirational.");
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Goal)
            .NotEmpty()
            .Must(g => AllowedGoals.Contains(g.Trim().ToLowerInvariant()))
            .WithMessage("Goal must be engagement, sales, or awareness.");
    }
}
