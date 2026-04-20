using MediatR;

namespace CaptionGen.Application.Social;

public sealed record ConnectLinkedInCallbackCommand(Guid UserId, string Code) : IRequest;
