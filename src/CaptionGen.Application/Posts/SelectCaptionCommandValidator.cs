using FluentValidation;

namespace CaptionGen.Application.Posts;

public sealed class SelectCaptionCommandValidator : AbstractValidator<SelectCaptionCommand>
{
    public SelectCaptionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PostId).NotEmpty();
        RuleFor(x => x.VariantIndex).GreaterThanOrEqualTo(0);
    }
}
