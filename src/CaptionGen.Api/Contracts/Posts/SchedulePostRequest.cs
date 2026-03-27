namespace CaptionGen.Api.Contracts.Posts;

public sealed class SchedulePostRequest
{
    public DateTime ScheduledAtUtc { get; init; }
    public int? SelectedCaptionIndex { get; init; }
    public IReadOnlyList<Guid>? MediaIds { get; init; }
}
