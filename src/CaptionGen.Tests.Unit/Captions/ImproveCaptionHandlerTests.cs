using CaptionGen.Application.Captions;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Captions;

public sealed class ImproveCaptionHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallAiService()
    {
        var ai = new Mock<IAiCaptionService>(MockBehavior.Strict);
        var expected = new CaptionImprovementResult("better", "short", "cta", "trace");

        ai.Setup(x => x.ImproveAsync(
                "caption",
                "instagram",
                "funny",
                "en",
                "engagement",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new ImproveCaptionHandler(ai.Object);

        var result = await sut.Handle(
            new ImproveCaptionCommand("caption", "instagram", "funny", "en", "engagement"),
            CancellationToken.None);

        result.Should().Be(expected);
        ai.VerifyAll();
    }

    [Fact]
    public async Task Handle_WithInvalidPlatform_ShouldThrow()
    {
        var ai = new Mock<IAiCaptionService>(MockBehavior.Strict);
        var sut = new ImproveCaptionHandler(ai.Object);

        var act = () => sut.Handle(
            new ImproveCaptionCommand("caption", "facebook", "funny", "en", "engagement"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*instagram, tiktok, or linkedin*");
    }
}
