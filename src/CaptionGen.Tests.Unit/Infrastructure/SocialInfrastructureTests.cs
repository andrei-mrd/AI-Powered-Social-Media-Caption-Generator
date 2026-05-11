using System.Net;
using System.Text;
using CaptionGen.Application.Social;
using CaptionGen.Domain.Social;
using CaptionGen.Infrastructure.Persistence;
using CaptionGen.Infrastructure.Social;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class SocialInfrastructureTests
{
    [Fact]
    public void BuildAuthorizationUrl_ShouldEncodeConfiguredValues()
    {
        var sut = BuildOAuthService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var url = sut.BuildAuthorizationUrl("state value");

        url.Should().StartWith("https://www.linkedin.com/oauth/v2/authorization");
        url.Should().Contain("client_id=client%20id");
        url.Should().Contain("redirect_uri=https%3A%2F%2Fapp.test%2Fcallback");
        url.Should().Contain("state=state%20value");
        url.Should().Contain("scope=openid%20profile%20w_member_social");
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldPostFormAndReturnTokens()
    {
        var handler = new StubHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "access_token": "access-token",
                  "expires_in": 3600,
                  "refresh_token": "refresh-token",
                  "refresh_token_expires_in": 7200
                }
                """,
                Encoding.UTF8,
                "application/json")
        });
        var sut = BuildOAuthService(handler);

        var result = await sut.ExchangeCodeAsync("auth-code", CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresInSeconds.Should().Be(3600);
        result.RefreshTokenExpiresInSeconds.Should().Be(7200);
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://www.linkedin.com/oauth/v2/accessToken");
        var form = await handler.LastRequest.Content!.ReadAsStringAsync();
        form.Should().Contain("grant_type=authorization_code");
        form.Should().Contain("code=auth-code");
    }

    [Fact]
    public async Task GetProfileAsync_ShouldSendBearerTokenAndReturnProfile()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"sub":"linkedin-user","name":"Demo User"}""", Encoding.UTF8, "application/json")
        });
        var sut = BuildOAuthService(handler);

        var result = await sut.GetProfileAsync("access-token", CancellationToken.None);

        result.Sub.Should().Be("linkedin-user");
        result.Name.Should().Be("Demo User");
        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("access-token");
    }

    [Fact]
    public async Task LinkedInPublisher_WhenResponseSucceeds_ShouldSendExpectedPayload()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = new LinkedInPublisher(new HttpClient(handler), new EchoTokenEncryptionService());
        var account = BuildSocialAccount();

        await sut.PublishAsync(account, "caption text", CancellationToken.None);

        sut.Platform.Should().Be("linkedin");
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://api.linkedin.com/v2/ugcPosts");
        handler.LastRequest.Headers.Authorization!.Parameter.Should().Be("encrypted-token");
        handler.LastRequest.Headers.GetValues("X-Restli-Protocol-Version").Should().Contain("2.0.0");
        handler.LastRequestBody.Should().Contain("urn:li:person:linkedin-user");
        handler.LastRequestBody.Should().Contain("caption text");
    }

    [Fact]
    public async Task LinkedInPublisher_WhenResponseFails_ShouldThrowWithResponseBody()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad payload")
        });
        var sut = new LinkedInPublisher(new HttpClient(handler), new EchoTokenEncryptionService());

        var act = () => sut.PublishAsync(BuildSocialAccount(), "caption", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*400*bad payload*");
    }

    [Fact]
    public async Task SocialAccountRepository_ShouldUpsertQueryAndDeleteAccounts()
    {
        await using var db = BuildDbContext();
        var sut = new SocialAccountRepository(db);
        var userId = Guid.NewGuid();

        await sut.UpsertAsync(BuildSocialAccount(userId, "Original"), CancellationToken.None);
        await sut.UpsertAsync(BuildSocialAccount(userId, "Updated"), CancellationToken.None);

        var allAccounts = await sut.GetByUserAsync(userId, CancellationToken.None);
        var account = await sut.GetByUserAndPlatformAsync(userId, "linkedin", CancellationToken.None);
        allAccounts.Should().ContainSingle();
        account.Should().NotBeNull();
        account!.DisplayName.Should().Be("Updated");

        await sut.DeleteAsync(userId, "linkedin", CancellationToken.None);

        (await sut.GetByUserAsync(userId, CancellationToken.None)).Should().BeEmpty();
    }

    private static LinkedInOAuthService BuildOAuthService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new LinkedInOptions
        {
            ClientId = "client id",
            ClientSecret = "client secret",
            RedirectUri = "https://app.test/callback"
        });

        return new LinkedInOAuthService(httpClient, options);
    }

    private static AppDbContext BuildDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SocialAccount BuildSocialAccount(Guid? userId = null, string displayName = "Demo User") =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Platform = "linkedin",
            PlatformUserId = "linkedin-user",
            DisplayName = displayName,
            AccessTokenEncrypted = "encrypted-token",
            RefreshTokenEncrypted = "encrypted-refresh",
            TokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            ConnectedAtUtc = DateTime.UtcNow
        };

    private sealed class EchoTokenEncryptionService : ITokenEncryptionService
    {
        public string Encrypt(string plaintext) => plaintext;

        public string Decrypt(string ciphertext) => ciphertext;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _responseFactory(request);
        }
    }
}
