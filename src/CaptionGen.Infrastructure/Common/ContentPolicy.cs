using System.Collections.ObjectModel;
using System.Linq;
using CaptionGen.Application.Common.Policies;
using Microsoft.Extensions.Options;

namespace CaptionGen.Infrastructure.Common;

public sealed class ContentPolicy : IContentPolicy
{
    private readonly HashSet<string> _platforms;
    private readonly HashSet<string> _tones;
    private readonly HashSet<string> _goals;
    private readonly HashSet<string> _captionLengths;

    private readonly ReadOnlyCollection<string> _platformsView;
    private readonly ReadOnlyCollection<string> _tonesView;
    private readonly ReadOnlyCollection<string> _goalsView;
    private readonly ReadOnlyCollection<string> _captionLengthsView;

    public ContentPolicy(IOptions<ContentPolicyOptions> options)
    {
        var cfg = options.Value;

        _platforms = BuildSet(cfg.Platforms);
        _tones = BuildSet(cfg.Tones);
        _goals = BuildSet(cfg.Goals);
        _captionLengths = BuildSet(cfg.CaptionLengths);

        _platformsView = new ReadOnlyCollection<string>(cfg.Platforms);
        _tonesView = new ReadOnlyCollection<string>(cfg.Tones);
        _goalsView = new ReadOnlyCollection<string>(cfg.Goals);
        _captionLengthsView = new ReadOnlyCollection<string>(cfg.CaptionLengths);

        MinCaptionCount = Math.Max(1, cfg.MinCaptionCount);
        MaxCaptionCount = Math.Max(MinCaptionCount, cfg.MaxCaptionCount);
        MinHashtags = Math.Max(1, cfg.MinHashtags);
        MaxHashtags = Math.Max(MinHashtags, cfg.MaxHashtags);
    }

    public IReadOnlyCollection<string> AllowedPlatforms => _platformsView;
    public IReadOnlyCollection<string> AllowedTones => _tonesView;
    public IReadOnlyCollection<string> AllowedGoals => _goalsView;
    public IReadOnlyCollection<string> AllowedCaptionLengths => _captionLengthsView;

    public int MinCaptionCount { get; }
    public int MaxCaptionCount { get; }
    public int MinHashtags { get; }
    public int MaxHashtags { get; }

    public bool IsSupportedPlatform(string value) => _platforms.Contains(Normalize(value));
    public bool IsSupportedTone(string value) => _tones.Contains(Normalize(value));
    public bool IsSupportedGoal(string value) => _goals.Contains(Normalize(value));
    public bool IsSupportedCaptionLength(string value) => _captionLengths.Contains(Normalize(value));

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static HashSet<string> BuildSet(IEnumerable<string> values) =>
        new(values.Select(v => (v ?? string.Empty).Trim().ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
}
