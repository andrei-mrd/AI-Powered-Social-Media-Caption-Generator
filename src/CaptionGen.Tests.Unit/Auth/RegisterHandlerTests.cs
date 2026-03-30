using CaptionGen.Application.Auth;
using CaptionGen.Application.Entitlements;
using CaptionGen.Application.Users;
using CaptionGen.Domain.Users;
using FluentAssertions;
using Moq;

namespace CaptionGen.Tests.Unit.Auth;

public sealed class RegisterHandlerTests
{
    [Fact]
    public async Task Handle_ShouldNormalizeEmail_AndPersistUser()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        users.Setup(x => x.AddAsync(It.Is<User>(u =>
                u.Email == "test@example.com" &&
                u.Id != Guid.Empty &&
                !string.IsNullOrWhiteSpace(u.PasswordHash)),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var entitlements = new Mock<IEntitlementService>(MockBehavior.Strict);
        entitlements.Setup(x => x.AssignPlanAsync(It.IsAny<Guid>(), "basic", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher.Setup(x => x.Hash("P@ssw0rd!")).Returns("hashed-password");

        var sut = new RegisterHandler(users.Object, entitlements.Object, passwordHasher.Object);

        var id = await sut.Handle(new RegisterCommand("  TEST@Example.com  ", "P@ssw0rd!"), CancellationToken.None);

        id.Should().NotBeEmpty();
        users.VerifyAll();
        entitlements.VerifyAll();
        passwordHasher.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyUsed_ShouldThrow()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow });

        var entitlements = new Mock<IEntitlementService>(MockBehavior.Loose);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Loose);

        var sut = new RegisterHandler(users.Object, entitlements.Object, passwordHasher.Object);

        var act = () => sut.Handle(new RegisterCommand("test@example.com", "pw"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already*");
        users.VerifyAll();
    }
}
