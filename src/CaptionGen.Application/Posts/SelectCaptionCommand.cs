using MediatR;

namespace CaptionGen.Application.Posts;

public sealed record SelectCaptionCommand(
    Guid UserId,
    Guid PostId,
    int VariantIndex) : IRequest;

public sealed class SelectCaptionHandler : IRequestHandler<SelectCaptionCommand>
{
    private readonly IPostRepository _posts;

    public SelectCaptionHandler(IPostRepository posts)
    {
        _posts = posts;
    }

    public async Task Handle(SelectCaptionCommand request, CancellationToken cancellationToken)
    {
        var post = await _posts.GetByIdWithCaptionsAndMediaAsync(request.PostId, request.UserId, cancellationToken);
        if (post is null) throw new InvalidOperationException("Post not found.");

        var caption = post.Captions.FirstOrDefault(c => c.VariantIndex == request.VariantIndex);
        if (caption is null)
            throw new InvalidOperationException("Caption not found.");

        foreach (var c in post.Captions)
        {
            c.IsSelected = c.VariantIndex == request.VariantIndex;
        }

        await _posts.SaveChangesAsync(cancellationToken);
    }
}
