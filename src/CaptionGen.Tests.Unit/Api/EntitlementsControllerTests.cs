using System.Security.Claims;
using CaptionGen.Api.Controllers;
using CaptionGen.Application.Entitlements;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CaptionGen.Tests.Unit.Api;

public sealed class EntitlementsControllerTests
{
    [Fact]
    public async Task Get_WithoutUserId_ShouldReturnUnauthorized()
    {
        var sut = new EntitlementsController(new Mock<IMediator>(MockBehavior.Strict).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.Get(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Get_WithUserId_ShouldReturnEntitlements()
    {
        var userId = Guid.NewGuid();
        var entitlements = new EntitlementDto("basic", "Basic", 30, 20, 1, true, true, 2, 3, DateTime.UtcNow, null);
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(new GetEntitlementsQuery(userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlements);
        var sut = new EntitlementsController(mediator.Object)
        {
            ControllerContext = BuildContext(userId)
        };

        var result = await sut.Get(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(entitlements);
        mediator.VerifyAll();
    }

    private static ControllerContext BuildContext(Guid userId) =>
        new()
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    "test"))
            }
        };
}
