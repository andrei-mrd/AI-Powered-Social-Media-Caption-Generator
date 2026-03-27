namespace CaptionGen.Application.Entitlements;

public sealed record EntitlementDto(
    string PlanSlug,
    string PlanName,
    int CaptionGenerationsPerMonth,
    int MediaAssetsLimit,
    int SeatsIncluded,
    bool SchedulingEnabled,
    bool AiImproveEnabled,
    DateTime? ActiveUntilUtc);
