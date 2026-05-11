namespace CaptionGen.Api.Contracts.Posts;

public sealed class SchedulePostRequest
{
    public required DateTime ScheduledAtUtc { get; init; }
    public int? SelectedCaptionIndex { get; init; }
    public IReadOnlyList<Guid>? MediaIds { get; init; }
}
