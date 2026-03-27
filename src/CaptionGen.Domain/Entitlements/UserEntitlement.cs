namespace CaptionGen.Domain.Entitlements;

public sealed class UserEntitlement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }

    public DateTime? ActiveUntilUtc { get; set; }
    public int SeatsInUse { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Plan Plan { get; set; } = null!;
}
