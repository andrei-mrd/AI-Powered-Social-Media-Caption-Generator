using System.ComponentModel.DataAnnotations;

namespace CaptionGen.Api.Contracts.Payments;

public sealed class CreateCheckoutSessionRequest
{
    [Required]
    public string PlanSlug { get; init; } = "";
}

public sealed class StripeWebhookRequest
{
    public string Payload { get; init; } = string.Empty;
}
