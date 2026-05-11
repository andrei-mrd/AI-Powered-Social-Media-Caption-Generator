using CaptionGen.Application.Entitlements;
using CaptionGen.Domain.Entitlements;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CaptionGen.Infrastructure.Entitlements;

public sealed class EntitlementService : IEntitlementService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntitlementService> _logger;
    private readonly IUsageService _usage;

    public EntitlementService(AppDbContext db, IUsageService usage, ILogger<EntitlementService> logger)
    {
        _db = db;
        _usage = usage;
        _logger = logger;
    }

    public async Task<EntitlementDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entitlement = await _db.UserEntitlements
            .Include(e => e.Plan)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        Plan? plan = entitlement?.Plan;

        if (plan is null && entitlement is not null)
        {
            plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == entitlement.PlanId, cancellationToken);
        }

        if (plan is null)
        {
            plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == "basic", cancellationToken)
                   ?? await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == "free", cancellationToken);
        }

        if (plan is null)
        {
            _logger.LogWarning("No plan found; returning fallback defaults for user {UserId}", userId);
            var fallbackUsage = await _usage.GetUsageAsync(userId, DateTime.UtcNow, cancellationToken);
            return new EntitlementDto(
                "free",
                "Free",
                30,
                20,
                1,
                true,
                true,
                fallbackUsage.CaptionsUsed,
                fallbackUsage.MediaUsed,
                fallbackUsage.PeriodStartUtc,
                null);
        }

        var usage = await _usage.GetUsageAsync(userId, DateTime.UtcNow, cancellationToken);
        return new EntitlementDto(
            plan.Slug,
            plan.Name,
            plan.CaptionGenerationsPerMonth,
            plan.MediaAssetsLimit,
            plan.SeatsIncluded,
            plan.SchedulingEnabled,
            plan.AiImproveEnabled,
            usage.CaptionsUsed,
            usage.MediaUsed,
            usage.PeriodStartUtc,
            entitlement?.ActiveUntilUtc);
    }

    public async Task AssignPlanAsync(Guid userId, string planSlug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = (planSlug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
            throw new InvalidOperationException("Plan slug is required.");

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Slug == normalizedSlug, cancellationToken);
        if (plan is null)
            throw new InvalidOperationException($"Plan '{planSlug}' not found.");

        var entitlement = await _db.UserEntitlements.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
        if (entitlement is null)
        {
            entitlement = new UserEntitlement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                ActiveUntilUtc = null,
                SeatsInUse = 1,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.UserEntitlements.Add(entitlement);
        }
        else
        {
            entitlement.PlanId = plan.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
