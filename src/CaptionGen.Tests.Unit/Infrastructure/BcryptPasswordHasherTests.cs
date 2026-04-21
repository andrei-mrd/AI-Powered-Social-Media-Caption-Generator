using CaptionGen.Infrastructure.Auth;
using FluentAssertions;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        _sut.Hash("password").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_ShouldNotReturnPlaintextPassword()
    {
        _sut.Hash("password").Should().NotBe("password");
    }

    [Fact]
    public void Hash_ShouldProduceDifferentHashEachTime()
    {
        var h1 = _sut.Hash("password");
        var h2 = _sut.Hash("password");

        h1.Should().NotBe(h2, because: "bcrypt uses a random salt per hash");
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hash = _sut.Hash("correct-password");

        _sut.Verify("correct-password", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _sut.Hash("correct-password");

        _sut.Verify("wrong-password", hash).Should().BeFalse();
    }
}
