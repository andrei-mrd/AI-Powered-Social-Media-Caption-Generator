using CaptionGen.Application.Entitlements;
using CaptionGen.Domain.Entitlements;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaptionGen.Infrastructure.Entitlements;

public sealed class UsageService : IUsageService
{
    private readonly AppDbContext _db;

    public UsageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task IncrementCaptionsAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0) return;
        var periodStart = GetPeriodStart(DateTime.UtcNow);
        var usage = await GetOrCreateAsync(userId, periodStart, cancellationToken);
        usage.CaptionsUsed += count;
        usage.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementMediaAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0) return;
        var periodStart = GetPeriodStart(DateTime.UtcNow);
        var usage = await GetOrCreateAsync(userId, periodStart, cancellationToken);
        usage.MediaUsed += count;
        usage.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UsageSnapshot> GetUsageAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var periodStart = GetPeriodStart(utcNow);
        var usage = await _db.UserUsages.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.PeriodStartUtc == periodStart, cancellationToken);

        return usage is null
            ? new UsageSnapshot(periodStart, 0, 0)
            : new UsageSnapshot(periodStart, usage.CaptionsUsed, usage.MediaUsed);
    }

    private async Task<UserUsage> GetOrCreateAsync(Guid userId, DateTime periodStart, CancellationToken ct)
    {
        var usage = await _db.UserUsages
            .FirstOrDefaultAsync(u => u.UserId == userId && u.PeriodStartUtc == periodStart, ct);

        if (usage is not null) return usage;

        usage = new UserUsage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PeriodStartUtc = periodStart,
            CaptionsUsed = 0,
            MediaUsed = 0,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.UserUsages.Add(usage);
        await _db.SaveChangesAsync(ct);
        return usage;
    }

    private static DateTime GetPeriodStart(DateTime utcNow)
    {
        return new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
