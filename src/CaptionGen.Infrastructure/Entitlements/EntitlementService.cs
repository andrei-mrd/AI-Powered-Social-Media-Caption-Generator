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

    public EntitlementService(AppDbContext db, ILogger<EntitlementService> logger)
    {
        _db = db;
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
            plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == "free", cancellationToken);
        }

        if (plan is null)
        {
            _logger.LogWarning("No plan found; returning fallback defaults for user {UserId}", userId);
            return new EntitlementDto(
                "free",
                "Free",
                30,
                20,
                1,
                true,
                true,
                null);
        }

        return new EntitlementDto(
            plan.Slug,
            plan.Name,
            plan.CaptionGenerationsPerMonth,
            plan.MediaAssetsLimit,
            plan.SeatsIncluded,
            plan.SchedulingEnabled,
            plan.AiImproveEnabled,
            entitlement?.ActiveUntilUtc);
    }
}
