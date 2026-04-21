using CaptionGen.Domain.Posts;

namespace CaptionGen.Application.Media;

public interface IMediaAssetRepository
{
    Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaAsset>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MediaAsset?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(MediaAsset asset, CancellationToken cancellationToken = default);
}
