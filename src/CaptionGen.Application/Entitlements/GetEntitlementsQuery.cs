using MediatR;

namespace CaptionGen.Application.Entitlements;

public sealed record GetEntitlementsQuery(Guid UserId) : IRequest<EntitlementDto>;

public sealed class GetEntitlementsHandler : IRequestHandler<GetEntitlementsQuery, EntitlementDto>
{
    private readonly IEntitlementService _service;

    public GetEntitlementsHandler(IEntitlementService service)
    {
        _service = service;
    }

    public Task<EntitlementDto> Handle(GetEntitlementsQuery request, CancellationToken cancellationToken)
        => _service.GetForUserAsync(request.UserId, cancellationToken);
}
