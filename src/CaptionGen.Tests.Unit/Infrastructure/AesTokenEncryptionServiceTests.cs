using CaptionGen.Infrastructure.Social;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class AesTokenEncryptionServiceTests
{
    private static AesTokenEncryptionService BuildSut(string? base64Key = null)
    {
        // 32 random bytes, base64-encoded
        base64Key ??= Convert.ToBase64String(new byte[32]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
        });

        return new AesTokenEncryptionService(
            Options.Create(new TokenEncryptionOptions { Key = base64Key }));
    }

    [Fact]
    public void EncryptThenDecrypt_ShouldReturnOriginalPlaintext()
    {
        var sut = BuildSut();
        var original = "access_token_abc123";

        var cipher = sut.Encrypt(original);
        var result = sut.Decrypt(cipher);

        result.Should().Be(original);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentOutputEachCall()
    {
        var sut = BuildSut();
        var token = "same_token";

        var cipher1 = sut.Encrypt(token);
        var cipher2 = sut.Encrypt(token);

        cipher1.Should().NotBe(cipher2, because: "each call uses a random nonce");
    }

    [Fact]
    public void Encrypt_ShouldReturnBase64String()
    {
        var sut = BuildSut();

        var cipher = sut.Encrypt("token");

        var act = () => Convert.FromBase64String(cipher);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WhenKeyMissing_ShouldThrow()
    {
        var act = () => new AesTokenEncryptionService(
            Options.Create(new TokenEncryptionOptions { Key = "" }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public void Constructor_WhenKeyWrongLength_ShouldThrow()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);

        var act = () => new AesTokenEncryptionService(
            Options.Create(new TokenEncryptionOptions { Key = shortKey }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*32 bytes*");
    }

    [Fact]
    public void Decrypt_WhenCiphertextTampered_ShouldThrow()
    {
        var sut = BuildSut();
        var cipher = sut.Encrypt("real_token");
        var bytes = Convert.FromBase64String(cipher);
        bytes[^1] ^= 0xFF; // flip last byte
        var tampered = Convert.ToBase64String(bytes);

        var act = () => sut.Decrypt(tampered);

        act.Should().Throw<Exception>();
    }
}
