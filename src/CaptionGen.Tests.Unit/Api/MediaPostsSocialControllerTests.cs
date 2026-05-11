using System.Security.Claims;
using System.Text;
using CaptionGen.Api.Contracts.Posts;
using CaptionGen.Api.Controllers;
using CaptionGen.Application.Captions;
using CaptionGen.Application.Media;
using CaptionGen.Application.Posts;
using CaptionGen.Application.Social;
using CaptionGen.Infrastructure.Social;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CaptionGen.Tests.Unit.Api;

public sealed class MediaPostsSocialControllerTests
{
    [Fact]
    public async Task MediaList_WithUserId_ShouldReturnItems()
    {
        var userId = Guid.NewGuid();
        var items = new[] { new MediaAssetDto(Guid.NewGuid(), "image", "https://cdn.test/1.jpg", DateTime.UtcNow) };
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new ListMediaQuery(userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        var sut = BuildMediaController(mediator, userId);

        var result = await sut.List(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(items);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task MediaUpload_WithValidFile_ShouldReturnUploadedMedia()
    {
        var userId = Guid.NewGuid();
        var media = new MediaAssetDto(Guid.NewGuid(), "image", "https://cdn.test/photo.jpg", DateTime.UtcNow);
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(
                It.Is<UploadMediaCommand>(command =>
                    command.UserId == userId &&
                    command.FileName == "photo.jpg" &&
                    command.ContentType == "image/jpeg" &&
                    command.Length == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);
        var sut = BuildMediaController(mediator, userId);
        var file = BuildFormFile("photo.jpg", "image/jpeg", "image");

        var result = await sut.Upload(file, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(media);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task MediaUpload_WhenFileMissing_ShouldReturnBadRequest()
    {
        var sut = BuildMediaController(new Mock<IMediator>(MockBehavior.Strict), Guid.NewGuid());

        var result = await sut.Upload(null!, CancellationToken.None);

        var objectResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Invalid file");
    }

    [Fact]
    public async Task MediaUpload_WhenValidationFails_ShouldReturnValidationProblem()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<UploadMediaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("File", "Unsupported file.")
            ]));
        var sut = BuildMediaController(mediator, Guid.NewGuid());

        var result = await sut.Upload(BuildFormFile("bad.txt", "text/plain", "bad"), CancellationToken.None);

        var problem = result.Should().BeAssignableTo<ObjectResult>().Subject.Value
            .Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("File");
    }

    [Fact]
    public async Task MediaDelete_WhenCommandSucceeds_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new DeleteMediaCommand(mediaId, userId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = BuildMediaController(mediator, userId);

        var result = await sut.Delete(mediaId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        mediator.VerifyAll();
    }

    [Fact]
    public void MediaPresign_ShouldReturnUploadUrlAndExpiry()
    {
        var sut = BuildMediaController(new Mock<IMediator>(MockBehavior.Strict), Guid.NewGuid());
        var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelper.SetupGet(x => x.ActionContext)
            .Returns(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()));
        urlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://api.test/media/upload");
        sut.Url = urlHelper.Object;

        var result = sut.Presign();

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<PresignMediaUploadResponse>().Subject;
        response.UploadUrl.Should().Be("https://api.test/media/upload");
        response.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task PostsGet_WithUserId_ShouldReturnPosts()
    {
        var userId = Guid.NewGuid();
        var posts = new[] { BuildPostDto(Guid.NewGuid()) };
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new GetPostsQuery(userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);
        var sut = BuildPostsController(mediator, userId);

        var result = await sut.Get(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(posts);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task PostsGet_WithoutUserId_ShouldReturnUnauthorized()
    {
        var sut = BuildPostsController(new Mock<IMediator>(MockBehavior.Strict));

        var result = await sut.Get(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task PostsSchedule_WithValidRequest_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var request = new SchedulePostRequest
        {
            ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
            SelectedCaptionIndex = 1,
            MediaIds = [mediaId]
        };
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(
                It.Is<SchedulePostCommand>(command =>
                    command.UserId == userId &&
                    command.PostId == postId &&
                    command.ScheduledAtUtc == request.ScheduledAtUtc &&
                    command.SelectedCaptionIndex == 1 &&
                    command.MediaIds.SequenceEqual(new[] { mediaId })),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = BuildPostsController(mediator, userId);

        var result = await sut.Schedule(postId, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task PostsSchedule_WhenValidationFails_ShouldReturnValidationProblem()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<SchedulePostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("ScheduledAtUtc", "Schedule time is required.")
            ]));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.Schedule(
            Guid.NewGuid(),
            new SchedulePostRequest { ScheduledAtUtc = DateTime.UtcNow.AddHours(1) },
            CancellationToken.None);

        var problem = result.Should().BeAssignableTo<ObjectResult>().Subject.Value
            .Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("ScheduledAtUtc");
    }

    [Fact]
    public async Task PostsSchedule_WhenInvalidOperationThrown_ShouldReturnBadRequest()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<SchedulePostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Schedule time must be in the future."));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.Schedule(
            Guid.NewGuid(),
            new SchedulePostRequest { ScheduledAtUtc = DateTime.UtcNow.AddHours(-1) },
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Schedule failed");
    }

    [Fact]
    public async Task PostsSelectCaption_WhenCommandSucceeds_ShouldReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new SelectCaptionCommand(userId, postId, 2), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = BuildPostsController(mediator, userId);

        var result = await sut.SelectCaption(postId, 2, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task PostsSelectCaption_WhenValidationFails_ShouldReturnValidationProblem()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<SelectCaptionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("VariantIndex", "Variant is invalid.")
            ]));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.SelectCaption(Guid.NewGuid(), -1, CancellationToken.None);

        var problem = result.Should().BeAssignableTo<ObjectResult>().Subject.Value
            .Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("VariantIndex");
    }

    [Fact]
    public async Task PostsSelectCaption_WhenInvalidOperationThrown_ShouldReturnBadRequest()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<SelectCaptionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Caption not found."));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.SelectCaption(Guid.NewGuid(), 9, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Selection failed");
    }

    [Fact]
    public async Task PostsCreate_WithValidRequest_ShouldReturnCreatedPost()
    {
        var userId = Guid.NewGuid();
        var response = new CreatePostResponse(
            Guid.NewGuid(),
            [new CaptionVariantDto(0, "caption", ["#tag"], "hook", "cta", 90)],
            ["#tag"],
            "trace");
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.Is<CreatePostCommand>(command =>
                    command.UserId == userId &&
                    command.Description == "description" &&
                    command.ForbiddenWords!.SequenceEqual(new[] { "spam" }) &&
                    command.KeywordsToInclude!.SequenceEqual(new[] { "launch" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var sut = BuildPostsController(mediator, userId);

        var result = await sut.Create(new CreatePostRequest
        {
            Description = "description",
            Platform = "instagram",
            Tone = "funny",
            Language = "en",
            Goal = "reach",
            ForbiddenWords = ["spam"],
            KeywordsToInclude = ["launch"]
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(response);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task PostsCreate_WhenAiServiceFails_ShouldReturnBadGateway()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<CreatePostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AiServiceException("service down", 503));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.Create(new CreatePostRequest { Description = "description" }, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(502);
        objectResult.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Caption generation failed");
    }

    [Fact]
    public async Task PostsCreate_WhenValidationFails_ShouldReturnValidationProblem()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<CreatePostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure("Description", "Description is required.")
            ]));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.Create(new CreatePostRequest { Description = "" }, CancellationToken.None);

        var problem = result.Should().BeAssignableTo<ObjectResult>().Subject.Value
            .Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public async Task PostsCreate_WhenInvalidOperationThrown_ShouldReturnBadRequest()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<CreatePostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid request."));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.Create(new CreatePostRequest { Description = "description" }, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        objectResult.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Invalid request");
    }

    [Fact]
    public async Task PostsCreate_WhenHttpRequestFails_ShouldReturnBadGateway()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<CreatePostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network failed"));
        var sut = BuildPostsController(mediator, Guid.NewGuid());

        var result = await sut.Create(new CreatePostRequest { Description = "description" }, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(502);
        objectResult.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("network failed");
    }

    [Fact]
    public void SocialConnectLinkedIn_ShouldSetStateCookieAndRedirect()
    {
        var linkedIn = new Mock<ILinkedInOAuthService>(MockBehavior.Strict);
        linkedIn.Setup(x => x.BuildAuthorizationUrl(It.Is<string>(state => state.Length == 32)))
            .Returns("https://linkedin.test/auth");
        var sut = BuildSocialController(new Mock<IMediator>(MockBehavior.Strict), linkedIn, Guid.NewGuid());

        var result = sut.ConnectLinkedIn();

        result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be("https://linkedin.test/auth");
        sut.Response.Headers.SetCookie.ToString().Should().Contain("linkedin_oauth_state=");
        linkedIn.VerifyAll();
    }

    [Fact]
    public async Task SocialCallback_WhenStateMatches_ShouldConnectAndRedirect()
    {
        var userId = Guid.NewGuid();
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new ConnectLinkedInCallbackCommand(userId, "code"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = BuildSocialController(mediator, new Mock<ILinkedInOAuthService>(MockBehavior.Strict), userId, "state");

        var result = await sut.LinkedInCallback("code", "state", null, CancellationToken.None);

        result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Contain("connected=linkedin");
        sut.Response.Headers.SetCookie.ToString().Should().Contain("linkedin_oauth_state=");
        mediator.VerifyAll();
    }

    [Fact]
    public async Task SocialCallback_WhenStateInvalid_ShouldReturnBadRequest()
    {
        var sut = BuildSocialController(
            new Mock<IMediator>(MockBehavior.Strict),
            new Mock<ILinkedInOAuthService>(MockBehavior.Strict),
            Guid.NewGuid(),
            "saved-state");

        var result = await sut.LinkedInCallback("code", "different", null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Subject.Value.Should().Be("Invalid OAuth state. Possible CSRF attempt.");
    }

    [Fact]
    public async Task SocialGetAccounts_WithUserId_ShouldReturnAccounts()
    {
        var userId = Guid.NewGuid();
        var accounts = new[] { new SocialAccountDto("linkedin", "Demo User", DateTime.UtcNow) };
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new GetSocialAccountsQuery(userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
        var sut = BuildSocialController(mediator, new Mock<ILinkedInOAuthService>(MockBehavior.Strict), userId);

        var result = await sut.GetAccounts(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(accounts);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task SocialDisconnect_ShouldLowercasePlatformAndReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new DisconnectSocialAccountCommand(userId, "linkedin"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = BuildSocialController(mediator, new Mock<ILinkedInOAuthService>(MockBehavior.Strict), userId);

        var result = await sut.Disconnect("LinkedIn", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        mediator.VerifyAll();
    }

    private static MediaController BuildMediaController(Mock<IMediator> mediator, Guid? userId = null) =>
        new(mediator.Object)
        {
            ControllerContext = BuildContext(userId)
        };

    private static PostsController BuildPostsController(Mock<IMediator> mediator, Guid? userId = null) =>
        new(mediator.Object)
        {
            ControllerContext = BuildContext(userId)
        };

    private static SocialController BuildSocialController(
        Mock<IMediator> mediator,
        Mock<ILinkedInOAuthService> linkedIn,
        Guid? userId = null,
        string? oauthStateCookie = null)
    {
        var context = BuildContext(userId).HttpContext;
        if (oauthStateCookie is not null)
        {
            context.Request.Headers.Cookie = $"linkedin_oauth_state={oauthStateCookie}";
        }

        return new SocialController(mediator.Object, linkedIn.Object, NullLogger<SocialController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static ControllerContext BuildContext(Guid? userId)
    {
        var context = new DefaultHttpContext();
        if (userId.HasValue)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                "test"));
        }

        return new ControllerContext { HttpContext = context };
    }

    private static IFormFile BuildFormFile(string fileName, string contentType, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static PostDto BuildPostDto(Guid postId) =>
        new(
            postId,
            "instagram",
            "draft",
            DateTime.UtcNow,
            null,
            "caption",
            ["#tag"],
            90,
            [new CaptionDto(0, "caption", true, ["#tag"], "hook", "cta", 90)],
            []);
}
