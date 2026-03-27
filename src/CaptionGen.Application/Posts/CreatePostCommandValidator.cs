using FluentValidation;

namespace CaptionGen.Application.Posts;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    private static readonly HashSet<string> AllowedPlatforms =
        new(StringComparer.OrdinalIgnoreCase) { "instagram", "tiktok", "linkedin" };

    private static readonly HashSet<string> AllowedTones =
        new(StringComparer.OrdinalIgnoreCase) { "funny", "professional", "inspirational" };

    public CreatePostCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Platform)
            .NotEmpty()
            .Must(p => AllowedPlatforms.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("Platform must be instagram, tiktok, or linkedin.");
        RuleFor(x => x.Tone)
            .NotEmpty()
            .Must(t => AllowedTones.Contains(t.Trim().ToLowerInvariant()))
            .WithMessage("Tone must be funny, professional, or inspirational.");
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Goal).NotEmpty();
        RuleFor(x => x.CaptionLength)
            .NotEmpty()
            .Must(l => l is "short" or "medium" or "long")
            .WithMessage("Caption length must be short, medium, or long.");
        RuleFor(x => x.IncludeEmojis).NotNull();
        RuleFor(x => x.IncludeCta).NotNull();
        RuleFor(x => x.HashtagCount).InclusiveBetween(5, 20);
        RuleFor(x => x.Count).InclusiveBetween(1, 10);
        RuleForEach(x => x.ForbiddenWords).NotEmpty();
        RuleForEach(x => x.KeywordsToInclude).NotEmpty();
    }
}
