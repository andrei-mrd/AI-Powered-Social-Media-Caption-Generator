using System.Linq;
using CaptionGen.Application.Media;
using MediatR;

namespace CaptionGen.Application.Posts;

public sealed class GetPostsHandler : IRequestHandler<GetPostsQuery, IReadOnlyList<PostDto>>
{
    private readonly IPostRepository _posts;
    private readonly IMediaStorageService _storage;

    public GetPostsHandler(IPostRepository posts, IMediaStorageService storage)
    {
        _posts = posts;
        _storage = storage;
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
                p.ScheduledAtUtc,
                p.Captions.FirstOrDefault(c => c.IsSelected)?.Text ?? p.Captions.OrderBy(c => c.VariantIndex).FirstOrDefault()?.Text,
                p.Captions
                    .OrderBy(c => c.VariantIndex)
                    .Select(c => new CaptionDto(c.VariantIndex, c.Text, c.IsSelected))
                    .ToList(),
                p.PostMedia
                    .Select(pm => pm.MediaAsset)
                    .Where(m => m != null)
                    .Select(m => new MediaDto(m!.Id, m.Type, _storage.BuildPublicUrl(m.StoragePath), m.CreatedAtUtc))
                    .ToList()))
            .ToList();
    }
}
