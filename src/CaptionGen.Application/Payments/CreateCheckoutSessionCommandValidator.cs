using FluentValidation;

namespace CaptionGen.Application.Payments;

public sealed class CreateCheckoutSessionCommandValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PlanSlug)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[a-zA-Z0-9-_]+$")
            .WithMessage("Plan slug may contain letters, numbers, dashes, and underscores only.");
    }
}
