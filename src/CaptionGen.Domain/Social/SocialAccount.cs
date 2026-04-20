namespace CaptionGen.Domain.Social;

public sealed class SocialAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string PlatformUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AccessTokenEncrypted { get; set; } = string.Empty;
    public string? RefreshTokenEncrypted { get; set; }
    public DateTime TokenExpiresAtUtc { get; set; }
    public DateTime ConnectedAtUtc { get; set; }
}
