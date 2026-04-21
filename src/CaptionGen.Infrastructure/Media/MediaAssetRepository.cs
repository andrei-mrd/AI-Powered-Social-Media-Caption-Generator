using System.Linq;
using CaptionGen.Application.Media;
using CaptionGen.Domain.Posts;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaptionGen.Infrastructure.Media;

public sealed class MediaAssetRepository : IMediaAssetRepository
{
    private readonly AppDbContext _db;

    public MediaAssetRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaAsset>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.MediaAssets
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<MediaAsset?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => _db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, cancellationToken);

    public async Task DeleteAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
