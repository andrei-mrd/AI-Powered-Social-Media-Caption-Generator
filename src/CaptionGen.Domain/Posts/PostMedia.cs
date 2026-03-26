namespace CaptionGen.Domain.Posts;

public sealed class PostMedia
{
    public Guid PostId { get; set; }
    public Guid MediaAssetId { get; set; }

    public Post Post { get; set; } = null!;
    public MediaAsset MediaAsset { get; set; } = null!;
}