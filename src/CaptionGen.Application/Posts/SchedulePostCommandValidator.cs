using FluentValidation;

namespace CaptionGen.Application.Posts;

public sealed class SchedulePostCommandValidator : AbstractValidator<SchedulePostCommand>
{
    public SchedulePostCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PostId).NotEmpty();
        RuleFor(x => x.ScheduledAtUtc)
            .Must(dt => dt > DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Schedule time must be in the future.");
        RuleFor(x => x.SelectedCaptionIndex)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SelectedCaptionIndex.HasValue);
        RuleFor(x => x.MediaIds)
            .NotNull();
    }
}

public sealed class SelectCaptionCommandValidator : AbstractValidator<SelectCaptionCommand>
{
    public SelectCaptionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PostId).NotEmpty();
        RuleFor(x => x.VariantIndex).GreaterThanOrEqualTo(0);
    }
}
