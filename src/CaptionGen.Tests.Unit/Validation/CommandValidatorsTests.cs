using CaptionGen.Application.Captions;
using CaptionGen.Application.Common.Policies;
using CaptionGen.Application.Media;
using CaptionGen.Application.Posts;
using CaptionGen.Application.Payments;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Validation;

public sealed class CommandValidatorsTests
{
    private static IContentPolicy BuildPolicy()
    {
        var policy = new Mock<IContentPolicy>(MockBehavior.Strict);
        policy.Setup(p => p.AllowedPlatforms).Returns(new[] { "instagram", "tiktok", "linkedin" });
        policy.Setup(p => p.AllowedTones).Returns(new[] { "funny", "professional", "casual" });
        policy.Setup(p => p.AllowedGoals).Returns(new[] { "reach", "engagement", "sales" });
        policy.Setup(p => p.AllowedCaptionLengths).Returns(new[] { "short", "medium", "long" });
        policy.Setup(p => p.MinCaptionCount).Returns(1);
        policy.Setup(p => p.MaxCaptionCount).Returns(10);
        policy.Setup(p => p.MinHashtags).Returns(1);
        policy.Setup(p => p.MaxHashtags).Returns(30);
        policy.Setup(p => p.IsSupportedPlatform(It.IsAny<string>()))
            .Returns<string>(v => new[] { "instagram", "tiktok", "linkedin" }.Contains(v, StringComparer.OrdinalIgnoreCase));
        policy.Setup(p => p.IsSupportedTone(It.IsAny<string>()))
            .Returns<string>(v => new[] { "funny", "professional", "casual" }.Contains(v, StringComparer.OrdinalIgnoreCase));
        policy.Setup(p => p.IsSupportedGoal(It.IsAny<string>()))
            .Returns<string>(v => new[] { "reach", "engagement", "sales" }.Contains(v, StringComparer.OrdinalIgnoreCase));
        policy.Setup(p => p.IsSupportedCaptionLength(It.IsAny<string>()))
            .Returns<string>(v => new[] { "short", "medium", "long" }.Contains(v, StringComparer.OrdinalIgnoreCase));
        return policy.Object;
    }

    [Fact]
    public void CreatePostCommandValidator_ShouldAcceptValidPayload()
    {
        var validator = new CreatePostCommandValidator(BuildPolicy());

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
        var validator = new CreatePostCommandValidator(BuildPolicy());

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
        var validator = new ImproveCaptionCommandValidator(BuildPolicy());

        var result = validator.Validate(new ImproveCaptionCommand(
            "caption",
            "instagram",
            "funny",
            "en",
            "conversion"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCheckoutSessionCommandValidator_ShouldAcceptSlug()
    {
        var validator = new CreateCheckoutSessionCommandValidator();

        var result = validator.Validate(new CreateCheckoutSessionCommand(Guid.NewGuid(), "agency"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCheckoutSessionCommandValidator_ShouldRejectBadSlug()
    {
        var validator = new CreateCheckoutSessionCommandValidator();

        var result = validator.Validate(new CreateCheckoutSessionCommand(Guid.NewGuid(), "bad slug!"));

        result.IsValid.Should().BeFalse();
    }
}
