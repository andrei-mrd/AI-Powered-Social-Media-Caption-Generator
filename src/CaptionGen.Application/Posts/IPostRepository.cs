using CaptionGen.Domain.Posts;

namespace CaptionGen.Application.Posts;

public interface IPostRepository
{
    Task AddAsync(Post post, CancellationToken cancellationToken);
    Task<IReadOnlyList<Post>> GetByUserWithCaptionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<Post?> GetByIdWithCaptionsAndMediaAsync(Guid postId, Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Post>> AcquireDueScheduledAsync(DateTime utcNow, int take, CancellationToken cancellationToken);
}
