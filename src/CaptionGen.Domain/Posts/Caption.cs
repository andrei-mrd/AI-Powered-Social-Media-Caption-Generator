namespace CaptionGen.Domain.Posts;

public sealed class Caption
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }

    public int VariantIndex { get; set; }
    public string Text { get; set; } = null!;
    public string? HashtagsText { get; set; }
    public string? Hook { get; set; }
    public string? Cta { get; set; }
    public bool IsSelected { get; set; }
    public int? Score { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Post Post { get; set; } = null!;
}
