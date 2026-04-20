using CaptionGen.Domain.Social;

namespace CaptionGen.Application.Social;

public interface ISocialPublisher
{
    string Platform { get; }
    Task PublishAsync(SocialAccount account, string text, CancellationToken ct);
}
