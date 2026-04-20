using CaptionGen.Domain.Social;

namespace CaptionGen.Application.Social;

public interface ISocialAccountRepository
{
    Task<IReadOnlyList<SocialAccount>> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<SocialAccount?> GetByUserAndPlatformAsync(Guid userId, string platform, CancellationToken ct);
    Task UpsertAsync(SocialAccount account, CancellationToken ct);
    Task DeleteAsync(Guid userId, string platform, CancellationToken ct);
}
