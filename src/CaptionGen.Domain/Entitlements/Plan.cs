namespace CaptionGen.Domain.Entitlements;

public sealed class Plan
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int CaptionGenerationsPerMonth { get; set; }
    public int MediaAssetsLimit { get; set; }
    public int SeatsIncluded { get; set; }

    public bool SchedulingEnabled { get; set; }
    public bool AiImproveEnabled { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
