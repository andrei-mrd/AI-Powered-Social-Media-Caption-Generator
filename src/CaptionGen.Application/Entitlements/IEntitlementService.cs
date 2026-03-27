namespace CaptionGen.Application.Entitlements;

public interface IEntitlementService
{
    Task<EntitlementDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
