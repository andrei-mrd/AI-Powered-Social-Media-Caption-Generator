using System.Security.Cryptography;
using System.Text;
using CaptionGen.Application.Payments;
using CaptionGen.Application.Entitlements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CaptionGen.Infrastructure.Payments;

public sealed class StripeWebhookService : IPaymentWebhookService
{
    private readonly StripeOptions _options;
    private readonly IEntitlementService _entitlements;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IOptions<StripeOptions> options,
        IEntitlementService entitlements,
        ILogger<StripeWebhookService> logger)
    {
        _options = options.Value;
        _entitlements = entitlements;
        _logger = logger;
    }

    public async Task HandleAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            _logger.LogError("Stripe webhook secret is not configured.");
            throw new InvalidOperationException("Stripe webhook secret is not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                _options.WebhookSecret,
                tolerance: 300,
                throwOnApiVersionMismatch: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Stripe webhook signature.");
            throw new PaymentServiceException("Invalid Stripe webhook signature.", true, ex);
        }

        const string checkoutCompleted = "checkout.session.completed";

        if (!string.Equals(stripeEvent.Type, checkoutCompleted, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Ignoring Stripe event type {Type}", stripeEvent.Type);
            return;
        }

        var session = stripeEvent.Data.Object as Session;
        if (session is null)
        {
            _logger.LogWarning("Stripe webhook payload missing session object.");
            return;
        }

        if (session.Metadata is null ||
            !session.Metadata.TryGetValue("userId", out var userIdRaw) ||
            !Guid.TryParse(userIdRaw, out var userId) ||
            !session.Metadata.TryGetValue("plan", out var planSlug))
        {
            _logger.LogWarning("Stripe session metadata missing userId/plan; skipping entitlement update.");
            return;
        }

        try
        {
            await _entitlements.AssignPlanAsync(userId, planSlug, cancellationToken);
            _logger.LogInformation("Updated entitlement for user {UserId} to plan {Plan} via Stripe webhook.", userId, planSlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update entitlement for user {UserId} from Stripe webhook.", userId);
            throw;
        }
    }
}
