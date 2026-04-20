using MediatR;

namespace CaptionGen.Application.Social;

public sealed class DisconnectSocialAccountHandler : IRequestHandler<DisconnectSocialAccountCommand>
{
    private readonly ISocialAccountRepository _repository;

    public DisconnectSocialAccountHandler(ISocialAccountRepository repository)
    {
        _repository = repository;
    }

    public Task Handle(DisconnectSocialAccountCommand request, CancellationToken cancellationToken)
        => _repository.DeleteAsync(request.UserId, request.Platform, cancellationToken);
}
