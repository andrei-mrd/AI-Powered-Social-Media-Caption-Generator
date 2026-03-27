namespace CaptionGen.Domain.Entitlements;

public sealed class UserUsage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public int CaptionsUsed { get; set; }
    public int MediaUsed { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
