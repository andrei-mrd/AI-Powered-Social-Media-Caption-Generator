namespace CaptionGen.Application.Payments;

public sealed record CheckoutSessionResult(string SessionId, string Url);

public interface IPaymentService
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid userId,
        string planSlug,
        CancellationToken cancellationToken = default);
}

public interface IPaymentWebhookService
{
    Task HandleAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default);
}

public sealed class PaymentServiceException : Exception
{
    public bool IsClientError { get; }

    public PaymentServiceException(string message, bool isClientError = false, Exception? innerException = null)
        : base(message, innerException)
    {
        IsClientError = isClientError;
    }
}
