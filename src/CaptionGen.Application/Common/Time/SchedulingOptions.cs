namespace CaptionGen.Application.Common.Time;

public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";

    /// <summary>
    /// IANA/Windows time zone id used for interpreting local schedule inputs (e.g., "Europe/Bucharest").
    /// </summary>
    public string LocalTimezone { get; init; } = "Europe/Bucharest";

    /// <summary>
    /// How often the background worker polls for due scheduled posts, in seconds.
    /// </summary>
    public int CheckIntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Maximum number of due posts processed per worker cycle.
    /// </summary>
    public int BatchSize { get; init; } = 10;
}
