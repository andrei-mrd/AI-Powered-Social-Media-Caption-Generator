namespace CaptionGen.Application.Entitlements;

public interface IEntitlementService
{
    Task<EntitlementDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignPlanAsync(Guid userId, string planSlug, CancellationToken cancellationToken = default);
}
