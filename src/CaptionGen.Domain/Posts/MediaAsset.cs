namespace CaptionGen.Domain.Posts;

public sealed class MediaAsset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Type { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }

    public List<PostMedia> PostMedia { get; set; } = new();
}