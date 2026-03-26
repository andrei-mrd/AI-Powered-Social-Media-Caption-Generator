using System.Linq;
using CaptionGen.Application.Posts;
using CaptionGen.Domain.Posts;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CaptionGen.Infrastructure.Posts;

public sealed class PostRepository : IPostRepository
{
    private readonly AppDbContext _db;

    public PostRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Post post, CancellationToken cancellationToken)
    {
        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetByUserWithCaptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.Posts
            .Where(p => p.UserId == userId)
            .Include(p => p.Captions)
            .Include(p => p.PostMedia).ThenInclude(pm => pm.MediaAsset)
            .OrderByDescending(p => p.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Post?> GetByIdWithCaptionsAndMediaAsync(Guid postId, Guid userId, CancellationToken cancellationToken)
    {
        return await _db.Posts
            .Include(p => p.Captions)
            .Include(p => p.PostMedia).ThenInclude(pm => pm.MediaAsset)
            .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> AcquireDueScheduledAsync(DateTime utcNow, int take, CancellationToken cancellationToken)
    {
        var posts = await _db.Posts
            .Where(p => p.Status == "scheduled" && p.ScheduledAtUtc <= utcNow)
            .OrderBy(p => p.ScheduledAtUtc)
            .Take(take)
            .Include(p => p.Captions)
            .Include(p => p.PostMedia).ThenInclude(pm => pm.MediaAsset)
            .ToListAsync(cancellationToken);

        foreach (var post in posts)
        {
            post.Status = "publishing";
        }

        if (posts.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return posts;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);
}
