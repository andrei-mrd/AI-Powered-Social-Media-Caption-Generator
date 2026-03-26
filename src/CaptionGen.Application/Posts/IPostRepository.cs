using CaptionGen.Domain.Posts;

namespace CaptionGen.Application.Posts;

public interface IPostRepository
{
    Task AddAsync(Post post, CancellationToken cancellationToken);
    Task<IReadOnlyList<Post>> GetByUserWithCaptionsAsync(Guid userId, CancellationToken cancellationToken);
}
