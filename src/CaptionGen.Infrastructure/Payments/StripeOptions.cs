namespace CaptionGen.Infrastructure.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Secret API key (test or live) used on the server. Must never be exposed to the client.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Publishable key for the frontend. Exposed via configuration only.
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Success redirect URL configured in Stripe Checkout (may include {CHECKOUT_SESSION_ID}).
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancel redirect URL configured in Stripe Checkout.
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Stripe webhook signing secret for checkout events.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Mapping from plan slugs (e.g., "agency") to Stripe Price IDs.
    /// </summary>
    public Dictionary<string, string> PriceIds { get; set; } = new();
}
