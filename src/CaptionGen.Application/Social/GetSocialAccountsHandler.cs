using MediatR;

namespace CaptionGen.Application.Social;

public sealed class GetSocialAccountsHandler : IRequestHandler<GetSocialAccountsQuery, IReadOnlyList<SocialAccountDto>>
{
    private readonly ISocialAccountRepository _repository;

    public GetSocialAccountsHandler(ISocialAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SocialAccountDto>> Handle(
        GetSocialAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetByUserAsync(request.UserId, cancellationToken);

        return accounts
            .Select(a => new SocialAccountDto(a.Platform, a.DisplayName, a.ConnectedAtUtc))
            .ToList();
    }
}
