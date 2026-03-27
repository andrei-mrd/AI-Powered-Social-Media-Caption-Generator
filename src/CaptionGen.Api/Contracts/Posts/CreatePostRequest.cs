namespace CaptionGen.Api.Contracts.Posts;

public sealed class CreatePostRequest
{
    public string Description { get; init; } = "";
    public string Platform { get; init; } = "";
    public string Tone { get; init; } = "funny";
    public string Language { get; init; } = "en";
    public string Goal { get; init; } = "awareness";
    public string CaptionLength { get; init; } = "medium";
    public bool IncludeEmojis { get; init; } = true;
    public bool IncludeCta { get; init; } = true;
    public int HashtagCount { get; init; } = 12;
    public string? Audience { get; init; }
    public string? BrandVoice { get; init; }
    public IReadOnlyList<string>? ForbiddenWords { get; init; }
    public IReadOnlyList<string>? KeywordsToInclude { get; init; }
    public int Count { get; init; } = 3;
}
