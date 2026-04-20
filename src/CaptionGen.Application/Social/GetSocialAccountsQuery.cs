using MediatR;

namespace CaptionGen.Application.Social;

public sealed record GetSocialAccountsQuery(Guid UserId) : IRequest<IReadOnlyList<SocialAccountDto>>;
