using System.Security.Claims;
using CaptionGen.Api.Controllers;
using CaptionGen.Application.Auth;
using CaptionGen.Infrastructure.Auth;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CaptionGen.Tests.Unit.Api;

public sealed class AuthControllerTests
{
    [Fact]
    public void Me_WithUserClaims_ShouldReturnCurrentUser()
    {
        var userId = Guid.NewGuid();
        var sut = BuildController();
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Email, "user@test.local")
                    ],
                    "test"))
            }
        };

        var result = sut.Me();

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<MeResponse>().Subject;
        response.Id.Should().Be(userId);
        response.Email.Should().Be("user@test.local");
    }

    [Fact]
    public void Me_WithoutClaims_ShouldReturnUnauthorized()
    {
        var sut = BuildController();
        sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = sut.Me();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public void Logout_ShouldDeleteCookieAndReturnOk()
    {
        var sut = BuildController();
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = sut.Logout();

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<LogoutResponse>().Subject;
        response.Ok.Should().BeTrue();
        sut.Response.Headers.SetCookie.ToString().Should().Contain("cg_at=");
    }

    private static AuthController BuildController() =>
        new(
            new Mock<IMediator>(MockBehavior.Loose).Object,
            Options.Create(new JwtOptions
            {
                CookieName = "cg_at",
                AccessMinutes = 60,
                AllowInsecureCookieOnHttp = true
            }),
            NullLogger<AuthController>.Instance);
}
