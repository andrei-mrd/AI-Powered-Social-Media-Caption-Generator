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
            .OrderByDescending(p => p.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
