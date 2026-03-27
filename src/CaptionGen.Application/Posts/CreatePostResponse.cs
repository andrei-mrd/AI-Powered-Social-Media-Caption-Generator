namespace CaptionGen.Application.Posts;

public sealed record CaptionVariantDto(
    int VariantIndex,
    string Text,
    IReadOnlyList<string> Hashtags,
    string? Hook,
    string? Cta,
    int? Score);

public sealed record CreatePostResponse(
    Guid Id,
    IReadOnlyList<CaptionVariantDto> Captions,
    IReadOnlyList<string> Hashtags,
    string? TraceId);
