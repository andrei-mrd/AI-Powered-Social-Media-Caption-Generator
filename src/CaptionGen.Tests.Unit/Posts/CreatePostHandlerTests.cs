using CaptionGen.Application.Captions;
using CaptionGen.Application.Posts;
using CaptionGen.Domain.Posts;
using FluentAssertions;
using Moq;
using CaptionGen.Application.Entitlements;

namespace CaptionGen.Tests.Unit.Posts;

public sealed class CreatePostHandlerTests
{
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
                    new GeneratedCaption("c1", new[] { "#a", "#b" }, "hook1", "cta1", 90),
                    new GeneratedCaption("c2", new[] { "#c", "#d" }, "hook2", "cta2", 85)
                },
                new[] { "#a", "#b" },
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
        response.Captions.First().Hashtags.Should().ContainInOrder("#a", "#b");
        response.Hashtags.Should().ContainInOrder("#a", "#b");
        response.TraceId.Should().Be("trace-1");
        response.Id.Should().NotBeEmpty();

        ai.VerifyAll();
        posts.VerifyAll();
        usage.VerifyAll();
    }

    [Theory]
    [InlineData("", "instagram", "funny", 1)]
    [InlineData("x", "nope", "funny", 1)]
    [InlineData("x", "instagram", "nope", 1)]
    [InlineData("x", "instagram", "funny", 0)]
    [InlineData("x", "instagram", "funny", 11)]
    public async Task Handle_WithInvalidRequest_ShouldThrow(string desc, string platform, string tone, int count)
    {
        var posts = new Mock<IPostRepository>(MockBehavior.Strict);
        var ai = new Mock<IAiCaptionService>(MockBehavior.Strict);
        var usage = new Mock<IUsageService>(MockBehavior.Loose);

        var sut = new CreatePostHandler(posts.Object, ai.Object, usage.Object);

        var act = () => sut.Handle(
            new CreatePostCommand(Guid.NewGuid(), desc, platform, tone, "en", "goal", "medium", true, true, 8, null, null, Array.Empty<string>(), Array.Empty<string>(), count),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
