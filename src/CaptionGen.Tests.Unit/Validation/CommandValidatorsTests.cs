using CaptionGen.Application.Captions;
using CaptionGen.Application.Media;
using CaptionGen.Application.Posts;
using FluentAssertions;

namespace CaptionGen.Tests.Unit.Validation;

public sealed class CommandValidatorsTests
{
    [Fact]
    public void CreatePostCommandValidator_ShouldAcceptValidPayload()
    {
        var validator = new CreatePostCommandValidator();

        var result = validator.Validate(new CreatePostCommand(
            Guid.NewGuid(),
            "hello world",
            "instagram",
            "funny",
            "en",
            "goal",
            "medium",
            true,
            true,
            10,
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            3));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreatePostCommandValidator_ShouldRejectBadPlatform()
    {
        var validator = new CreatePostCommandValidator();

        var result = validator.Validate(new CreatePostCommand(
            Guid.NewGuid(),
            "hello world",
            "facebook",
            "funny",
            "en",
            "goal",
            "medium",
            true,
            true,
            10,
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            3));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePostCommand.Platform));
    }

    [Fact]
    public void SchedulePostCommandValidator_ShouldRejectPastDate()
    {
        var validator = new SchedulePostCommandValidator();

        var result = validator.Validate(new SchedulePostCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddMinutes(-5),
            0,
            Array.Empty<Guid>()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SelectCaptionCommandValidator_ShouldRequireNonNegativeIndex()
    {
        var validator = new SelectCaptionCommandValidator();

        var result = validator.Validate(new SelectCaptionCommand(Guid.NewGuid(), Guid.NewGuid(), -1));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UploadMediaCommandValidator_ShouldRejectUnsupportedType()
    {
        var validator = new UploadMediaCommandValidator();

        var result = validator.Validate(new UploadMediaCommand(
            Guid.NewGuid(),
            "file.txt",
            "text/plain",
            10,
            new MemoryStream(new byte[] { 1, 2, 3 })));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ImproveCaptionCommandValidator_ShouldRejectBadGoal()
    {
        var validator = new ImproveCaptionCommandValidator();

        var result = validator.Validate(new ImproveCaptionCommand(
            "caption",
            "instagram",
            "funny",
            "en",
            "conversion"));

        result.IsValid.Should().BeFalse();
    }
}
