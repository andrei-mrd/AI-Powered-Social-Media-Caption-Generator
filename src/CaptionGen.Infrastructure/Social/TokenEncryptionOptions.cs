namespace CaptionGen.Infrastructure.Social;

public sealed class TokenEncryptionOptions
{
    public const string SectionName = "TokenEncryption";

    /// <summary>Base64-encoded 32-byte key for AES-256-GCM.</summary>
    public string Key { get; init; } = string.Empty;
}
