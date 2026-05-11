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

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken);
                return;
            case "customer.subscription.created":
            case "customer.subscription.updated":
                await HandleSubscriptionChangedAsync(stripeEvent, cancellationToken);
                return;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                return;
            default:
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Ignoring Stripe event type {Type}", stripeEvent.Type);
                }
                return;
        }
    }

    private async Task HandleCheckoutCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null)
        {
            _logger.LogWarning("Stripe webhook payload missing checkout session object.");
            return;
        }

        if (!TryResolveUserId(session, out var userId))
        {
            _logger.LogWarning("Stripe checkout session {SessionId} missing user mapping; skipping entitlement update.",
                session.Id);
            return;
        }

        if (!TryResolvePlanSlug(session, out var planSlug))
        {
            _logger.LogWarning("Stripe checkout session {SessionId} missing plan metadata; skipping entitlement update.",
                session.Id);
            return;
        }

        await AssignPlanAsync(userId, planSlug, stripeEvent.Type, cancellationToken);
    }

    private async Task HandleSubscriptionChangedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null)
        {
            _logger.LogWarning("Stripe webhook payload missing subscription object for event {Type}.", stripeEvent.Type);
            return;
        }

        if (!TryResolveUserId(subscription, out var userId))
        {
            _logger.LogWarning("Stripe subscription {SubscriptionId} missing user metadata; skipping entitlement update.",
                subscription.Id);
            return;
        }

        if (!TryResolvePlanSlug(subscription, out var planSlug))
        {
            _logger.LogWarning(
                "Stripe subscription {SubscriptionId} did not match any configured plan; skipping entitlement update.",
                subscription.Id);
            return;
        }

        await AssignPlanAsync(userId, planSlug, stripeEvent.Type, cancellationToken);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null)
        {
            _logger.LogWarning("Stripe webhook payload missing subscription object for deletion event.");
            return;
        }

        if (!TryResolveUserId(subscription, out var userId))
        {
            _logger.LogWarning("Stripe subscription deletion {SubscriptionId} missing user metadata; skipping downgrade.",
                subscription.Id);
            return;
        }

        await AssignPlanAsync(userId, "basic", stripeEvent.Type, cancellationToken);
    }

    private async Task AssignPlanAsync(
        Guid userId,
        string planSlug,
        string eventType,
        CancellationToken cancellationToken)
    {
        try
        {
            await _entitlements.AssignPlanAsync(userId, planSlug, cancellationToken);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Updated entitlement for user {UserId} to plan {Plan} from Stripe event {EventType}.",
                    userId,
                    planSlug,
                    eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update entitlement for user {UserId} from Stripe event {EventType}.",
                userId,
                eventType);
            throw new PaymentServiceException(
                $"Failed to update entitlement from Stripe event '{eventType}'.",
                false,
                ex);
        }
    }

    private static bool TryResolveUserId(Session session, out Guid userId)
    {
        if (TryReadGuid(session.Metadata, "userId", out userId))
        {
            return true;
        }

        return Guid.TryParse(session.ClientReferenceId, out userId);
    }

    private static bool TryResolveUserId(Subscription subscription, out Guid userId)
        => TryReadGuid(subscription.Metadata, "userId", out userId);

    private static bool TryResolvePlanSlug(Session session, out string planSlug)
        => TryReadNormalized(session.Metadata, "plan", out planSlug);

    private bool TryResolvePlanSlug(Subscription subscription, out string planSlug)
    {
        var priceId = subscription.Items?.Data?
            .Select(item => item.Price?.Id)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

        return TryResolvePlanSlugFromPriceId(priceId, out planSlug);
    }

    private bool TryResolvePlanSlugFromPriceId(string? priceId, out string planSlug)
    {
        planSlug = string.Empty;

        if (string.IsNullOrWhiteSpace(priceId) || _options.PriceIds.Count == 0)
        {
            return false;
        }

        var match = _options.PriceIds.FirstOrDefault(kv =>
            string.Equals(kv.Value, priceId, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(match.Key))
        {
            return false;
        }

        planSlug = match.Key.Trim().ToLowerInvariant();
        return true;
    }

    private static bool TryReadGuid(IReadOnlyDictionary<string, string>? metadata, string key, out Guid value)
    {
        value = Guid.Empty;
        return TryReadNormalized(metadata, key, out var raw) && Guid.TryParse(raw, out value);
    }

    private static bool TryReadNormalized(IReadOnlyDictionary<string, string>? metadata, string key, out string value)
    {
        value = string.Empty;

        if (metadata is null || !metadata.TryGetValue(key, out var raw))
        {
            return false;
        }

        var normalized = raw?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        value = normalized.ToLowerInvariant();
        return true;
    }
}
