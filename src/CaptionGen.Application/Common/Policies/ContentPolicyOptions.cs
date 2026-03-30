using System.Collections.Generic;

namespace CaptionGen.Application.Common.Policies;

public sealed class ContentPolicyOptions
{
    public const string SectionName = "ContentPolicy";

    public List<string> Platforms { get; init; } = new() { "instagram", "tiktok", "linkedin" };
    public List<string> Tones { get; init; } = new() { "funny", "professional", "inspirational" };
    public List<string> Goals { get; init; } = new() { "engagement", "sales", "awareness" };
    public List<string> CaptionLengths { get; init; } = new() { "short", "medium", "long" };

    public int MinCaptionCount { get; init; } = 1;
    public int MaxCaptionCount { get; init; } = 10;

    public int MinHashtags { get; init; } = 5;
    public int MaxHashtags { get; init; } = 20;
}
