using CaptionGen.Application.Auth;
using CaptionGen.Application.Users;
using CaptionGen.Domain.Users;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Auth;

public sealed class LoginHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnToken()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var tokens = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed-pw",
            CreatedAtUtc = DateTime.UtcNow
        };

        users.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        tokens.Setup(x => x.CreateAccessToken(user)).Returns("token-123");
        passwordHasher.Setup(x => x.Verify("pw", "hashed-pw")).Returns(true);

        var sut = new LoginHandler(users.Object, tokens.Object, passwordHasher.Object);

        var token = await sut.Handle(new LoginCommand(" TEST@EXAMPLE.COM ", "pw"), CancellationToken.None);

        token.Should().Be("token-123");
        users.VerifyAll();
        tokens.VerifyAll();
        passwordHasher.VerifyAll();
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrow()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var tokens = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed-pw",
            CreatedAtUtc = DateTime.UtcNow
        };

        users.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher.Setup(x => x.Verify("wrong", "hashed-pw")).Returns(false);

        var sut = new LoginHandler(users.Object, tokens.Object, passwordHasher.Object);

        var act = () => sut.Handle(new LoginCommand("test@example.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid credentials*");
        tokens.Verify(x => x.CreateAccessToken(It.IsAny<User>()), Times.Never);
        users.VerifyAll();
        passwordHasher.VerifyAll();
    }
}

