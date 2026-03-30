using MediatR;

namespace CaptionGen.Application.Payments;

public sealed record CreateCheckoutSessionCommand(Guid UserId, string PlanSlug)
    : IRequest<CheckoutSessionResult>;

public sealed class CreateCheckoutSessionHandler
    : IRequestHandler<CreateCheckoutSessionCommand, CheckoutSessionResult>
{
    private readonly IPaymentService _payments;

    public CreateCheckoutSessionHandler(IPaymentService payments)
    {
        _payments = payments;
    }

    public async Task<CheckoutSessionResult> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var plan = (request.PlanSlug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(plan))
            throw new InvalidOperationException("Plan is required.");

        return await _payments.CreateCheckoutSessionAsync(request.UserId, plan, cancellationToken);
    }
}
