using MediatR;

namespace CaptionGen.Application.Social;

public sealed record DisconnectSocialAccountCommand(Guid UserId, string Platform) : IRequest;
