using CaptionGen.Application.Social;
using CaptionGen.Domain.Social;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Social;

public sealed class ConnectLinkedInCallbackHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoExistingAccount_ShouldCreateAndUpsert()
    {
        var oauth = new Mock<ILinkedInOAuthService>(MockBehavior.Strict);
        var repo = new Mock<ISocialAccountRepository>(MockBehavior.Strict);
        var encryption = new Mock<ITokenEncryptionService>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var tokens = new LinkedInTokens("access-token", "refresh-token", 3600, 86400);
        var profile = new LinkedInProfile("li-sub-123", "John Doe");

        oauth.Setup(x => x.ExchangeCodeAsync("auth-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        oauth.Setup(x => x.GetProfileAsync("access-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        repo.Setup(x => x.GetByUserAndPlatformAsync(userId, "linkedin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialAccount?)null);
        repo.Setup(x => x.UpsertAsync(It.Is<SocialAccount>(a =>
                a.UserId == userId &&
                a.Platform == "linkedin" &&
                a.PlatformUserId == "li-sub-123" &&
                a.DisplayName == "John Doe"),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        encryption.Setup(x => x.Encrypt("access-token")).Returns("enc-access");
        encryption.Setup(x => x.Encrypt("refresh-token")).Returns("enc-refresh");

        var sut = new ConnectLinkedInCallbackHandler(oauth.Object, repo.Object, encryption.Object);

        await sut.Handle(new ConnectLinkedInCallbackCommand(userId, "auth-code"), CancellationToken.None);

        oauth.VerifyAll();
        repo.VerifyAll();
        encryption.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenAccountAlreadyExists_ShouldUpdateTokens()
    {
        var oauth = new Mock<ILinkedInOAuthService>(MockBehavior.Strict);
        var repo = new Mock<ISocialAccountRepository>(MockBehavior.Strict);
        var encryption = new Mock<ITokenEncryptionService>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var existing = new SocialAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Platform = "linkedin",
            ConnectedAtUtc = DateTime.UtcNow.AddDays(-30),
            PlatformUserId = "old-sub",
            DisplayName = "Old Name",
            AccessTokenEncrypted = "old-enc"
        };

        oauth.Setup(x => x.ExchangeCodeAsync("code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedInTokens("new-access", null, 3600, 0));
        oauth.Setup(x => x.GetProfileAsync("new-access", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedInProfile("new-sub", "New Name"));

        repo.Setup(x => x.GetByUserAndPlatformAsync(userId, "linkedin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repo.Setup(x => x.UpsertAsync(It.Is<SocialAccount>(a =>
                a.Id == existing.Id &&
                a.DisplayName == "New Name" &&
                a.RefreshTokenEncrypted == null),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        encryption.Setup(x => x.Encrypt("new-access")).Returns("enc-new-access");

        var sut = new ConnectLinkedInCallbackHandler(oauth.Object, repo.Object, encryption.Object);

        await sut.Handle(new ConnectLinkedInCallbackCommand(userId, "code"), CancellationToken.None);

        repo.VerifyAll();
        existing.DisplayName.Should().Be("New Name");
        existing.PlatformUserId.Should().Be("new-sub");
    }
}

public sealed class DisconnectSocialAccountHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepositoryDelete()
    {
        var repo = new Mock<ISocialAccountRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();

        repo.Setup(x => x.DeleteAsync(userId, "linkedin", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new DisconnectSocialAccountHandler(repo.Object);

        await sut.Handle(new DisconnectSocialAccountCommand(userId, "linkedin"), CancellationToken.None);

        repo.VerifyAll();
    }
}

public sealed class GetSocialAccountsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMappedDtos()
    {
        var repo = new Mock<ISocialAccountRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();
        var connectedAt = DateTime.UtcNow.AddDays(-1);

        var accounts = new[]
        {
            new SocialAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Platform = "linkedin",
                DisplayName = "John Doe",
                ConnectedAtUtc = connectedAt,
                AccessTokenEncrypted = "enc",
                PlatformUserId = "li-123"
            }
        };

        repo.Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var sut = new GetSocialAccountsHandler(repo.Object);

        var result = await sut.Handle(new GetSocialAccountsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Platform.Should().Be("linkedin");
        result[0].DisplayName.Should().Be("John Doe");
        result[0].ConnectedAtUtc.Should().Be(connectedAt);
        repo.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenNoAccounts_ShouldReturnEmptyList()
    {
        var repo = new Mock<ISocialAccountRepository>(MockBehavior.Strict);
        var userId = Guid.NewGuid();

        repo.Setup(x => x.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SocialAccount>());

        var sut = new GetSocialAccountsHandler(repo.Object);

        var result = await sut.Handle(new GetSocialAccountsQuery(userId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
