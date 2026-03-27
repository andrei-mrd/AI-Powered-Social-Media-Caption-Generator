using CaptionGen.Application.Media;
using CaptionGen.Application.Posts;
using CaptionGen.Domain.Posts;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Posts;

public sealed class GetPostsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldOrderCaptionsByVariantIndex()
    {
        var posts = new Mock<IPostRepository>(MockBehavior.Strict);
        var storage = new Mock<IMediaStorageService>(MockBehavior.Strict);
        storage.Setup(x => x.BuildPublicUrl(It.IsAny<string>()))
            .Returns<string>(p => $"url/{p}");

        var userId = Guid.NewGuid();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = "instagram",
            Status = "draft",
            CreatedAtUtc = DateTime.UtcNow,
            Captions = new List<Caption>
            {
                new() { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), VariantIndex = 2, Text = "c2", CreatedAtUtc = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), VariantIndex = 0, Text = "c0", CreatedAtUtc = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), VariantIndex = 1, Text = "c1", CreatedAtUtc = DateTime.UtcNow }
            }
        };

        posts.Setup(x => x.GetByUserWithCaptionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { post });

        var sut = new GetPostsHandler(posts.Object, storage.Object);

        var result = await sut.Handle(new GetPostsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Captions.Select(c => c.VariantIndex).Should().ContainInOrder(0, 1, 2);
        result[0].Captions.Select(c => c.Text).Should().ContainInOrder("c0", "c1", "c2");
        posts.VerifyAll();
    }
}
