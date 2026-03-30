using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CaptionGen.Application.Common.Policies;

public interface IContentPolicy
{
    IReadOnlyCollection<string> AllowedPlatforms { get; }
    IReadOnlyCollection<string> AllowedTones { get; }
    IReadOnlyCollection<string> AllowedGoals { get; }
    IReadOnlyCollection<string> AllowedCaptionLengths { get; }

    int MinCaptionCount { get; }
    int MaxCaptionCount { get; }
    int MinHashtags { get; }
    int MaxHashtags { get; }

    bool IsSupportedPlatform(string value);
    bool IsSupportedTone(string value);
    bool IsSupportedGoal(string value);
    bool IsSupportedCaptionLength(string value);
}
