using System.IdentityModel.Tokens.Jwt;
using CaptionGen.Domain.Users;
using CaptionGen.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class JwtTokenServiceTests
{
    private static JwtTokenService BuildSut() =>
        new(Options.Create(new JwtOptions
        {
            Key = "super-secret-key-that-is-long-enough-32ch",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessMinutes = 60
        }));

    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = "x",
        CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public void CreateAccessToken_ShouldReturnNonEmptyString()
    {
        var token = BuildSut().CreateAccessToken(MakeUser());

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateAccessToken_ShouldBeValidJwt()
    {
        var token = BuildSut().CreateAccessToken(MakeUser());

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void CreateAccessToken_ShouldContainUserIdAndEmail()
    {
        var user = MakeUser();
        var token = BuildSut().CreateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Value == user.Email);
    }

    [Fact]
    public void CreateAccessToken_ShouldHaveCorrectIssuerAndAudience()
    {
        var token = BuildSut().CreateAccessToken(MakeUser());

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void CreateAccessToken_ShouldExpireInFuture()
    {
        var token = BuildSut().CreateAccessToken(MakeUser());

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
}
