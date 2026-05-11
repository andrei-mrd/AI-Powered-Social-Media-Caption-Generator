using CaptionGen.Application.Captions;
using CaptionGen.Application.Posts;
using CaptionGen.Domain.Posts;
using FluentAssertions;
using Moq;
using CaptionGen.Application.Entitlements;

namespace CaptionGen.Tests.Unit.Posts;

public sealed class CreatePostHandlerTests
{
    private static readonly string[] FirstCaptionHashtags = ["#a", "#b"];
    private static readonly string[] SecondCaptionHashtags = ["#c", "#d"];

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallAi_AndPersistPost()
    {
        var posts = new Mock<IPostRepository>(MockBehavior.Strict);
        var ai = new Mock<IAiCaptionService>(MockBehavior.Strict);
        var usage = new Mock<IUsageService>(MockBehavior.Strict);

        var userId = Guid.NewGuid();

        ai.Setup(x => x.GenerateAsync(
                "hello",
                "instagram",
                "funny",
                2,
                It.IsAny<CaptionGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaptionGenerationResult(
                new[]
                {
                    new GeneratedCaption("c1", FirstCaptionHashtags, "hook1", "cta1", 90),
                    new GeneratedCaption("c2", SecondCaptionHashtags, "hook2", "cta2", 85)
                },
                FirstCaptionHashtags,
                88,
                "reason",
                "trace-1"));

        posts.Setup(x => x.AddAsync(It.Is<Post>(p =>
                p.UserId == userId &&
                p.Platform == "instagram" &&
                p.Status == "draft" &&
                p.Captions.Count == 2 &&
                p.Captions.All(c => c.PostId == p.Id)),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        usage.Setup(x => x.IncrementCaptionsAsync(userId, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new CreatePostHandler(posts.Object, ai.Object, usage.Object);

        var response = await sut.Handle(
            new CreatePostCommand(userId, "  hello  ", " Instagram ", " FUNNY ", "en", "reach", "medium", true, true, 8, null, null, Array.Empty<string>(), Array.Empty<string>(), 2),
            CancellationToken.None);

        response.Captions.Select(c => c.Text).Should().ContainInOrder("c1", "c2");
        response.Captions[0].Hashtags.Should().ContainInOrder("#a", "#b");
        response.Hashtags.Should().ContainInOrder("#a", "#b");
        response.TraceId.Should().Be("trace-1");
        response.Id.Should().NotBeEmpty();

        ai.VerifyAll();
        posts.VerifyAll();
        usage.VerifyAll();
    }

    // Input validation (platform, tone, count, etc.) is enforced by CreatePostCommandValidator
    // via the MediatR pipeline before the handler runs. Validation is tested in CommandValidatorsTests.
}
