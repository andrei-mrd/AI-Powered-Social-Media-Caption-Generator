using CaptionGen.Application.Common.Validation;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace CaptionGen.Tests.Unit.Application;

public sealed record ValidationTestRequest(string Value) : IRequest<string>;

public sealed class AlwaysPassValidator : AbstractValidator<ValidationTestRequest>
{
    public AlwaysPassValidator() { }
}

public sealed class AlwaysFailValidator : AbstractValidator<ValidationTestRequest>
{
    public AlwaysFailValidator()
    {
        RuleFor(x => x.Value).Must(_ => false).WithMessage("Value is required");
    }
}

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_ShouldCallNext()
    {
        var sut = new ValidationBehavior<ValidationTestRequest, string>(Array.Empty<IValidator<ValidationTestRequest>>());
        var nextCalled = false;

        var result = await sut.Handle(
            new ValidationTestRequest("x"),
            ct => { nextCalled = true; return Task.FromResult("ok"); },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidatorPasses_ShouldCallNext()
    {
        var sut = new ValidationBehavior<ValidationTestRequest, string>(new IValidator<ValidationTestRequest>[] { new AlwaysPassValidator() });
        var nextCalled = false;

        await sut.Handle(
            new ValidationTestRequest("valid"),
            ct => { nextCalled = true; return Task.FromResult("ok"); },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_ShouldThrowValidationException()
    {
        var sut = new ValidationBehavior<ValidationTestRequest, string>(new IValidator<ValidationTestRequest>[] { new AlwaysFailValidator() });

        var act = () => sut.Handle(
            new ValidationTestRequest(""),
            ct => Task.FromResult("ok"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Value is required*");
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_ShouldNotCallNext()
    {
        var sut = new ValidationBehavior<ValidationTestRequest, string>(new IValidator<ValidationTestRequest>[] { new AlwaysFailValidator() });
        var nextCalled = false;

        try
        {
            await sut.Handle(
                new ValidationTestRequest(""),
                ct => { nextCalled = true; return Task.FromResult("ok"); },
                CancellationToken.None);
        }
        catch (ValidationException) { }

        nextCalled.Should().BeFalse();
    }
}
