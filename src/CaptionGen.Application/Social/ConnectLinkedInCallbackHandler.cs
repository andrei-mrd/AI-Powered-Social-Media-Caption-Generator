using CaptionGen.Domain.Social;
using MediatR;

namespace CaptionGen.Application.Social;

public sealed class ConnectLinkedInCallbackHandler : IRequestHandler<ConnectLinkedInCallbackCommand>
{
    private readonly ILinkedInOAuthService _oauth;
    private readonly ISocialAccountRepository _repository;
    private readonly ITokenEncryptionService _encryption;

    public ConnectLinkedInCallbackHandler(
        ILinkedInOAuthService oauth,
        ISocialAccountRepository repository,
        ITokenEncryptionService encryption)
    {
        _oauth = oauth;
        _repository = repository;
        _encryption = encryption;
    }

    public async Task Handle(ConnectLinkedInCallbackCommand request, CancellationToken cancellationToken)
    {
        var tokens = await _oauth.ExchangeCodeAsync(request.Code, cancellationToken);
        var profile = await _oauth.GetProfileAsync(tokens.AccessToken, cancellationToken);

        var existing = await _repository.GetByUserAndPlatformAsync(request.UserId, "linkedin", cancellationToken);

        var account = existing ?? new SocialAccount
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Platform = "linkedin",
            ConnectedAtUtc = DateTime.UtcNow
        };

        account.PlatformUserId = profile.Sub;
        account.DisplayName = profile.Name;
        account.AccessTokenEncrypted = _encryption.Encrypt(tokens.AccessToken);
        account.RefreshTokenEncrypted = tokens.RefreshToken is not null
            ? _encryption.Encrypt(tokens.RefreshToken)
            : null;
        account.TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresInSeconds);

        await _repository.UpsertAsync(account, cancellationToken);
    }
}
