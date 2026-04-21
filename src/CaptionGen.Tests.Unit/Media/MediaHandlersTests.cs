using System.Text;
using CaptionGen.Application.Media;
using CaptionGen.Domain.Posts;
using FluentAssertions;
using Moq;
using CaptionGen.Application.Entitlements;

namespace CaptionGen.Tests.Unit.Media;

public sealed class MediaHandlersTests
{
    [Fact]
    public async Task UploadMedia_ShouldPersistAssetAndReturnDto()
    {
        var repo = new Mock<IMediaAssetRepository>(MockBehavior.Strict);
        var storage = new Mock<IMediaStorageService>(MockBehavior.Strict);
        var usage = new Mock<IUsageService>(MockBehavior.Strict);

        storage.Setup(x => x.SaveAsync(
                It.IsAny<Stream>(),
                "photo.jpg",
                "image/jpeg",
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredMedia("path/photo.jpg", "http://files/path/photo.jpg", "image/jpeg"));

        repo.Setup(x => x.AddAsync(It.Is<MediaAsset>(m =>
                m.Type == "image" &&
                m.StoragePath == "path/photo.jpg" &&
                m.UserId != Guid.Empty),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        usage.Setup(x => x.IncrementMediaAsync(It.IsAny<Guid>(), 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new UploadMediaHandler(repo.Object, storage.Object, usage.Object);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        var result = await sut.Handle(
            new UploadMediaCommand(Guid.NewGuid(), "photo.jpg", "image/jpeg", stream.Length, stream),
            CancellationToken.None);

        result.Url.Should().Be("http://files/path/photo.jpg");
        result.Type.Should().Be("image");
        result.Id.Should().NotBeEmpty();

        storage.VerifyAll();
        repo.VerifyAll();
        usage.VerifyAll();
    }

    [Fact]
    public async Task UploadMedia_WithUnsupportedType_ShouldThrow()
    {
        var repo = new Mock<IMediaAssetRepository>(MockBehavior.Loose);
        var storage = new Mock<IMediaStorageService>(MockBehavior.Loose);
        var usage = new Mock<IUsageService>(MockBehavior.Loose);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        var sut = new UploadMediaHandler(repo.Object, storage.Object, usage.Object);

        var act = () => sut.Handle(
            new UploadMediaCommand(Guid.NewGuid(), "doc.txt", "text/plain", stream.Length, stream),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unsupported media type*");
    }

    [Fact]
    public async Task DeleteMedia_ShouldDeleteFromStorageAndRepo()
    {
        var repo = new Mock<IMediaAssetRepository>(MockBehavior.Strict);
        var storage = new Mock<IMediaStorageService>(MockBehavior.Strict);

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StoragePath = "path/file.jpg",
            Type = "image",
            CreatedAtUtc = DateTime.UtcNow
        };

        repo.Setup(x => x.GetByIdAsync(asset.Id, asset.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        storage.Setup(x => x.DeleteAsync(asset.StoragePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(x => x.DeleteAsync(asset, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new DeleteMediaHandler(repo.Object, storage.Object);

        await sut.Handle(new DeleteMediaCommand(asset.Id, asset.UserId), CancellationToken.None);

        repo.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task DeleteMedia_WhenNotFound_ShouldThrow()
    {
        var repo = new Mock<IMediaAssetRepository>(MockBehavior.Strict);
        var storage = new Mock<IMediaStorageService>(MockBehavior.Loose);

        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaAsset?)null);

        var sut = new DeleteMediaHandler(repo.Object, storage.Object);

        var act = () => sut.Handle(new DeleteMediaCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Media not found*");
    }
}
