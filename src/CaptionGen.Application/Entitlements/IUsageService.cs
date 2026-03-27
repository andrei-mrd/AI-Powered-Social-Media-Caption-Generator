namespace CaptionGen.Application.Entitlements;

public interface IUsageService
{
    Task IncrementCaptionsAsync(Guid userId, int count, CancellationToken cancellationToken = default);
    Task IncrementMediaAsync(Guid userId, int count, CancellationToken cancellationToken = default);
    Task<UsageSnapshot> GetUsageAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);
}

public sealed record UsageSnapshot(
    DateTime PeriodStartUtc,
    int CaptionsUsed,
    int MediaUsed);
