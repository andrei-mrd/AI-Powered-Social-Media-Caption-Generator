using CaptionGen.Application.Captions;
using CaptionGen.Application.Posts;
using CaptionGen.Domain.Posts;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Posts;

public sealed class CreatePostHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallAi_AndPersistPost()
    {
        var posts = new Mock<IPostRepository>(MockBehavior.Strict);
        var ai = new Mock<IAiCaptionService>(MockBehavior.Strict);

        var userId = Guid.NewGuid();

        ai.Setup(x => x.GenerateAsync(
                "hello",
                "instagram",
                "funny",
                2,
                It.IsAny<CaptionGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaptionGenerationResult(
                new[] { "c1", "c2", "c3" },
                new[] { "#a", "#b" }));

        posts.Setup(x => x.AddAsync(It.Is<Post>(p =>
                p.UserId == userId &&
                p.Platform == "instagram" &&
                p.Status == "draft" &&
                p.Captions.Count == 2 &&
                p.Captions.All(c => c.PostId == p.Id)),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new CreatePostHandler(posts.Object, ai.Object);

        var response = await sut.Handle(
            new CreatePostCommand(userId, "  hello  ", " Instagram ", " FUNNY ", "en", "reach", "medium", true, true, 8, null, null, Array.Empty<string>(), Array.Empty<string>(), 2),
            CancellationToken.None);

        response.Captions.Should().ContainInOrder("c1", "c2", "c3");
        response.Hashtags.Should().ContainInOrder("#a", "#b");
        response.Id.Should().NotBeEmpty();

        ai.VerifyAll();
        posts.VerifyAll();
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

        var sut = new CreatePostHandler(posts.Object, ai.Object);

        var act = () => sut.Handle(
            new CreatePostCommand(Guid.NewGuid(), desc, platform, tone, "en", "goal", "medium", true, true, 8, null, null, Array.Empty<string>(), Array.Empty<string>(), count),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
