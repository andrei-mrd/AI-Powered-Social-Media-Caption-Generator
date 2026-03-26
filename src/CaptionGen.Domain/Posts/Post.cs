namespace CaptionGen.Domain.Posts;

public sealed class Post
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Description { get; set; } = string.Empty;
    public string Platform { get; set; } = null!;
    public string Tone { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public string Status { get; set; } = null!;

    public List<Caption> Captions { get; set; } = new();
    public List<PostMedia> PostMedia { get; set; } = new();
}
