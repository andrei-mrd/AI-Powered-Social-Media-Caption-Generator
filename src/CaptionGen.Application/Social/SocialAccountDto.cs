namespace CaptionGen.Application.Social;

public sealed record SocialAccountDto(
    string Platform,
    string DisplayName,
    DateTime ConnectedAtUtc);
