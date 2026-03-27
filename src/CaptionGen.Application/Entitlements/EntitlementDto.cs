namespace CaptionGen.Application.Entitlements;

public sealed record EntitlementDto(
    string PlanSlug,
    string PlanName,
    int CaptionGenerationsPerMonth,
    int MediaAssetsLimit,
    int SeatsIncluded,
    bool SchedulingEnabled,
    bool AiImproveEnabled,
    int CaptionsUsedThisPeriod,
    int MediaUsedThisPeriod,
    DateTime PeriodStartUtc,
    DateTime? ActiveUntilUtc);
