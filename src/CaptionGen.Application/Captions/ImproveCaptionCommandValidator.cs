using CaptionGen.Application.Common.Policies;
using FluentValidation;

namespace CaptionGen.Application.Captions;

public sealed class ImproveCaptionCommandValidator : AbstractValidator<ImproveCaptionCommand>
{
    public ImproveCaptionCommandValidator(IContentPolicy policy)
    {
        RuleFor(x => x.Caption).NotEmpty();
        RuleFor(x => x.Platform)
            .NotEmpty()
            .Must(policy.IsSupportedPlatform)
            .WithMessage($"Platform must be one of: {string.Join(", ", policy.AllowedPlatforms)}.");
        RuleFor(x => x.Tone)
            .NotEmpty()
            .Must(policy.IsSupportedTone)
            .WithMessage($"Tone must be one of: {string.Join(", ", policy.AllowedTones)}.");
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Goal)
            .NotEmpty()
            .Must(policy.IsSupportedGoal)
            .WithMessage($"Goal must be one of: {string.Join(", ", policy.AllowedGoals)}.");
    }
}
