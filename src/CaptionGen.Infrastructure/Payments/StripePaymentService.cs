using CaptionGen.Application.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CaptionGen.Infrastructure.Payments;

public sealed class StripePaymentService : IPaymentService
{
    private readonly StripeOptions _options;
    private readonly SessionService _sessions;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IOptions<StripeOptions> options,
        IStripeClient client,
        ILogger<StripePaymentService> logger)
    {
        _options = options.Value;
        _sessions = new SessionService(client);
        _logger = logger;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid userId,
        string planSlug,
        CancellationToken cancellationToken = default)
    {
        var priceId = ResolvePriceId(planSlug);
        var successUrl = EnsureUrl(_options.SuccessUrl, "Stripe:SuccessUrl");
        var cancelUrl = EnsureUrl(_options.CancelUrl, "Stripe:CancelUrl");

        try
        {
            var session = await _sessions.CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "subscription",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = priceId,
                            Quantity = 1
                        }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "plan", planSlug }
                    }
                },
                requestOptions: null,
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
            {
                throw new PaymentServiceException("Stripe session was created without an id or url.");
            }

            _logger.LogInformation("Created Stripe checkout session {SessionId} for user {UserId} plan {Plan}",
                session.Id, userId, planSlug);

            return new CheckoutSessionResult(session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            var status = (int?)ex.HttpStatusCode;
            var clientError = status is >= 400 and < 500;
            _logger.LogWarning(ex, "Stripe API error while creating session for user {UserId}", userId);
            throw new PaymentServiceException(ex.Message, clientError, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating Stripe session for user {UserId}", userId);
            throw new PaymentServiceException("Failed to create checkout session.", false, ex);
        }
    }

    private string ResolvePriceId(string planSlug)
    {
        if (_options.PriceIds is null || _options.PriceIds.Count == 0)
            throw new PaymentServiceException("Stripe price map is empty. Check configuration.");

        var match = _options.PriceIds
            .FirstOrDefault(kv => string.Equals(kv.Key, planSlug, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(match.Value))
            throw new PaymentServiceException($"Unknown plan '{planSlug}'.", isClientError: true);

        return match.Value;
    }

    private static string EnsureUrl(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PaymentServiceException($"{name} is missing.");

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new PaymentServiceException($"{name} must be an absolute URL.");

        return uri.ToString();
    }
}
