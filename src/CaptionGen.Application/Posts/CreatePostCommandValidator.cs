using CaptionGen.Application.Common.Policies;
using FluentValidation;

namespace CaptionGen.Application.Posts;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator(IContentPolicy policy)
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Platform)
            .NotEmpty()
            .Must(policy.IsSupportedPlatform)
            .WithMessage($"Platform must be one of: {string.Join(", ", policy.AllowedPlatforms)}.");
        RuleFor(x => x.Tone)
            .NotEmpty()
            .Must(policy.IsSupportedTone)
            .WithMessage($"Tone must be one of: {string.Join(", ", policy.AllowedTones)}.");
        RuleFor(x => x.Language).NotEmpty();
        RuleFor(x => x.Goal).NotEmpty();
        RuleFor(x => x.CaptionLength)
            .NotEmpty()
            .Must(policy.IsSupportedCaptionLength)
            .WithMessage($"Caption length must be one of: {string.Join(", ", policy.AllowedCaptionLengths)}.");
        RuleFor(x => x.IncludeEmojis).NotNull();
        RuleFor(x => x.IncludeCta).NotNull();
        RuleFor(x => x.HashtagCount).InclusiveBetween(policy.MinHashtags, policy.MaxHashtags);
        RuleFor(x => x.Count).InclusiveBetween(policy.MinCaptionCount, policy.MaxCaptionCount);
        RuleForEach(x => x.ForbiddenWords).NotEmpty();
        RuleForEach(x => x.KeywordsToInclude).NotEmpty();
    }
}
