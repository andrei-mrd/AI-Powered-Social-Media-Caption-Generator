using CaptionGen.Application.Common.Time;
using CaptionGen.Application.Posts;
using CaptionGen.Domain.Posts;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Posts;

public sealed class SchedulePostHandlerTests
{
    [Fact]
    public async Task Handle_WithValidData_ShouldScheduleAndSelectCaption()
    {
        var repo = new Mock<IPostRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var scheduled = DateTime.UtcNow.AddMinutes(30);

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Status = "draft",
            CreatedAtUtc = DateTime.UtcNow,
            Captions = new List<Caption>
            {
                new() { Id = Guid.NewGuid(), PostId = postId, VariantIndex = 0, Text = "c0", CreatedAtUtc = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), PostId = postId, VariantIndex = 1, Text = "c1", CreatedAtUtc = DateTime.UtcNow }
            },
            PostMedia = new List<PostMedia>()
        };

        repo.Setup(x => x.GetByIdWithCaptionsAndMediaAsync(postId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var timezone = new Mock<ITimezoneService>(MockBehavior.Strict);
        timezone.Setup(x => x.ToUtc(It.IsAny<DateTime>())).Returns<DateTime>(dt => dt);

        var sut = new SchedulePostHandler(repo.Object, timezone.Object);

        await sut.Handle(
            new SchedulePostCommand(userId, postId, scheduled, 1, new[] { mediaId }),
            CancellationToken.None);

        post.Status.Should().Be("scheduled");
        post.ScheduledAtUtc.Should().NotBeNull();
        post.Captions.Single(c => c.VariantIndex == 1).IsSelected.Should().BeTrue();
        post.Captions.Single(c => c.VariantIndex == 0).IsSelected.Should().BeFalse();
        post.PostMedia.Should().ContainSingle();
        post.PostMedia[0].MediaAssetId.Should().Be(mediaId);

        repo.VerifyAll();
    }

    [Fact]
    public async Task Handle_WithMissingCaption_ShouldThrow()
    {
        var repo = new Mock<IPostRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Status = "draft",
            CreatedAtUtc = DateTime.UtcNow,
            Captions = new List<Caption>
            {
                new() { Id = Guid.NewGuid(), PostId = postId, VariantIndex = 0, Text = "c0", CreatedAtUtc = DateTime.UtcNow }
            }
        };

        repo.Setup(x => x.GetByIdWithCaptionsAndMediaAsync(postId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var timezone = new Mock<ITimezoneService>(MockBehavior.Strict);
        timezone.Setup(x => x.ToUtc(It.IsAny<DateTime>())).Returns<DateTime>(dt => dt);

        var sut = new SchedulePostHandler(repo.Object, timezone.Object);

        var act = () => sut.Handle(
            new SchedulePostCommand(userId, postId, DateTime.UtcNow.AddMinutes(5), 5, Array.Empty<Guid>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Selected caption not found*");
    }

    [Fact]
    public async Task Handle_WithPastDate_ShouldThrow()
    {
        var repo = new Mock<IPostRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Status = "draft",
            CreatedAtUtc = DateTime.UtcNow,
            Captions = new List<Caption>
            {
                new() { Id = Guid.NewGuid(), PostId = postId, VariantIndex = 0, Text = "c0", CreatedAtUtc = DateTime.UtcNow }
            }
        };

        repo.Setup(x => x.GetByIdWithCaptionsAndMediaAsync(postId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var timezone = new Mock<ITimezoneService>(MockBehavior.Strict);
        timezone.Setup(x => x.ToUtc(It.IsAny<DateTime>())).Returns<DateTime>(dt => dt);

        var sut = new SchedulePostHandler(repo.Object, timezone.Object);

        var act = () => sut.Handle(
            new SchedulePostCommand(userId, postId, DateTime.UtcNow.AddMinutes(-10), 0, Array.Empty<Guid>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*future*");
    }
}

public sealed class SelectCaptionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMarkSelectedCaption()
    {
        var repo = new Mock<IPostRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Status = "draft",
            CreatedAtUtc = DateTime.UtcNow,
            Captions = new List<Caption>
            {
                new() { Id = Guid.NewGuid(), PostId = postId, VariantIndex = 0, Text = "c0", CreatedAtUtc = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), PostId = postId, VariantIndex = 1, Text = "c1", CreatedAtUtc = DateTime.UtcNow }
            }
        };

        repo.Setup(x => x.GetByIdWithCaptionsAndMediaAsync(postId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        repo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new SelectCaptionHandler(repo.Object);

        await sut.Handle(new SelectCaptionCommand(userId, postId, 1), CancellationToken.None);

        post.Captions.Single(c => c.VariantIndex == 1).IsSelected.Should().BeTrue();
        post.Captions.Single(c => c.VariantIndex == 0).IsSelected.Should().BeFalse();
        repo.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenCaptionMissing_ShouldThrow()
    {
        var repo = new Mock<IPostRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Status = "draft",
            CreatedAtUtc = DateTime.UtcNow,
            Captions = new List<Caption>
            {
                new() { Id = Guid.NewGuid(), PostId = postId, VariantIndex = 0, Text = "c0", CreatedAtUtc = DateTime.UtcNow }
            }
        };

        repo.Setup(x => x.GetByIdWithCaptionsAndMediaAsync(postId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        var sut = new SelectCaptionHandler(repo.Object);

        var act = () => sut.Handle(new SelectCaptionCommand(userId, postId, 5), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Caption not found*");
    }
}
