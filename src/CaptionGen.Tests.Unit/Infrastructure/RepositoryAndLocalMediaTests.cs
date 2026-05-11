using System.Text;
using CaptionGen.Domain.Posts;
using CaptionGen.Domain.Users;
using CaptionGen.Infrastructure.Media;
using CaptionGen.Infrastructure.Persistence;
using CaptionGen.Infrastructure.Posts;
using CaptionGen.Infrastructure.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class RepositoryAndLocalMediaTests
{
    [Fact]
    public async Task UserRepository_ShouldAddAndFindUserByEmail()
    {
        await using var db = BuildDbContext();
        var sut = new UserRepository(db);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.local",
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow
        };

        await sut.AddAsync(user, CancellationToken.None);
        var result = await sut.GetByEmailAsync(user.Email, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task MediaAssetRepository_ShouldListGetAndDeleteAssetsForUser()
    {
        await using var db = BuildDbContext();
        var sut = new MediaAssetRepository(db);
        var userId = Guid.NewGuid();
        var older = BuildMediaAsset(userId, DateTime.UtcNow.AddMinutes(-5));
        var newer = BuildMediaAsset(userId, DateTime.UtcNow);
        var otherUserAsset = BuildMediaAsset(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5));

        await sut.AddAsync(older, CancellationToken.None);
        await sut.AddAsync(newer, CancellationToken.None);
        await sut.AddAsync(otherUserAsset, CancellationToken.None);

        var list = await sut.ListByUserAsync(userId, CancellationToken.None);
        var loaded = await sut.GetByIdAsync(newer.Id, userId, CancellationToken.None);
        list.Select(asset => asset.Id).Should().ContainInOrder(newer.Id, older.Id);
        loaded.Should().NotBeNull();

        await sut.DeleteAsync(newer, CancellationToken.None);

        (await sut.GetByIdAsync(newer.Id, userId, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task PostRepository_ShouldLoadPostsWithCaptionsAndMedia()
    {
        await using var db = BuildDbContext();
        var sut = new PostRepository(db);
        var userId = Guid.NewGuid();
        var post = BuildPost(userId, "draft", DateTime.UtcNow);
        var media = BuildMediaAsset(userId, DateTime.UtcNow);
        post.Captions.Add(new Caption
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            VariantIndex = 0,
            Text = "caption",
            CreatedAtUtc = DateTime.UtcNow
        });
        post.PostMedia.Add(new PostMedia
        {
            PostId = post.Id,
            MediaAssetId = media.Id,
            MediaAsset = media
        });

        await sut.AddAsync(post, CancellationToken.None);

        var posts = await sut.GetByUserWithCaptionsAsync(userId, CancellationToken.None);
        var loaded = await sut.GetByIdWithCaptionsAndMediaAsync(post.Id, userId, CancellationToken.None);

        posts.Should().ContainSingle();
        posts[0].Captions.Should().ContainSingle();
        posts[0].PostMedia.Should().ContainSingle();
        loaded.Should().NotBeNull();
        loaded!.Captions.Should().ContainSingle();
    }

    [Fact]
    public async Task PostRepository_ShouldAcquireDueScheduledPostsAndMarkPublishing()
    {
        await using var db = BuildDbContext();
        var sut = new PostRepository(db);
        var userId = Guid.NewGuid();
        var due = BuildPost(userId, "scheduled", DateTime.UtcNow.AddMinutes(-1));
        var future = BuildPost(userId, "scheduled", DateTime.UtcNow.AddMinutes(10));
        var draft = BuildPost(userId, "draft", DateTime.UtcNow.AddMinutes(-2));
        due.ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-1);
        future.ScheduledAtUtc = DateTime.UtcNow.AddMinutes(10);
        draft.ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-2);
        db.Posts.AddRange(due, future, draft);
        await db.SaveChangesAsync();

        var acquired = await sut.AcquireDueScheduledAsync(DateTime.UtcNow, 10, CancellationToken.None);

        acquired.Should().ContainSingle(post => post.Id == due.Id);
        (await db.Posts.FindAsync(due.Id))!.Status.Should().Be("publishing");
        (await db.Posts.FindAsync(future.Id))!.Status.Should().Be("scheduled");
        await sut.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task LocalMediaStorageService_ShouldSaveBuildUrlAndDeleteFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "captiongen-tests", Guid.NewGuid().ToString("N"));
        var sut = new LocalMediaStorageService(Options.Create(new MediaStorageOptions
        {
            RootPath = root,
            PublicBaseUrl = "https://cdn.test/media"
        }));

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("image"));
        var result = await sut.SaveAsync(stream, "Photo.JPG", "image/jpeg", stream.Length, CancellationToken.None);

        result.MediaType.Should().Be("image/jpeg");
        result.PublicUrl.Should().StartWith("https://cdn.test/media/");
        File.Exists(Path.Combine(root, result.StoragePath)).Should().BeTrue();
        sut.BuildPublicUrl("/nested/file.jpg").Should().Be("https://cdn.test/media/nested/file.jpg");

        await sut.DeleteAsync(result.StoragePath, CancellationToken.None);

        File.Exists(Path.Combine(root, result.StoragePath)).Should().BeFalse();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task LocalMediaStorageService_WhenDeletePathMissing_ShouldNoOp()
    {
        var sut = new LocalMediaStorageService(Options.Create(new MediaStorageOptions()));

        await sut.DeleteAsync("", CancellationToken.None);

        true.Should().BeTrue();
    }

    private static AppDbContext BuildDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static MediaAsset BuildMediaAsset(Guid userId, DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "image",
            StoragePath = $"{Guid.NewGuid():N}.jpg",
            CreatedAtUtc = createdAtUtc
        };

    private static Post BuildPost(Guid userId, string status, DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = "description",
            Platform = "linkedin",
            Tone = "professional",
            Language = "en",
            Goal = "engagement",
            Status = status,
            CreatedAtUtc = createdAtUtc
        };
}
