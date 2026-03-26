using System.Linq;
using MediatR;

namespace CaptionGen.Application.Posts;

public sealed class GetPostsHandler : IRequestHandler<GetPostsQuery, IReadOnlyList<PostDto>>
{
    private readonly IPostRepository _posts;

    public GetPostsHandler(IPostRepository posts)
    {
        _posts = posts;
    }

    public async Task<IReadOnlyList<PostDto>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
    {
        var items = await _posts.GetByUserWithCaptionsAsync(request.UserId, cancellationToken);

        return items
            .Select(p => new PostDto(
                p.Id,
                p.Platform,
                p.Status,
                p.CreatedAtUtc,
                p.Captions
                    .OrderBy(c => c.VariantIndex)
                    .Select(c => new CaptionDto(c.VariantIndex, c.Text))
                    .ToList()))
            .ToList();
    }
}
