using CaptionGen.Application.Common.Time;
using CaptionGen.Domain.Posts;
using MediatR;

namespace CaptionGen.Application.Posts;

public sealed record SchedulePostCommand(
    Guid UserId,
    Guid PostId,
    DateTime ScheduledAtUtc,
    int? SelectedCaptionIndex,
    IReadOnlyList<Guid> MediaIds) : IRequest;

public sealed class SchedulePostHandler : IRequestHandler<SchedulePostCommand>
{
    private readonly IPostRepository _posts;
    private readonly ITimezoneService _timezoneService;

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "draft", "scheduled", "publishing", "published", "failed" };

    public SchedulePostHandler(IPostRepository posts, ITimezoneService timezoneService)
    {
        _posts = posts;
        _timezoneService = timezoneService;
    }

    public async Task Handle(SchedulePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _posts.GetByIdWithCaptionsAndMediaAsync(request.PostId, request.UserId, cancellationToken);
        if (post is null) throw new InvalidOperationException("Post not found.");

        if (!AllowedStatuses.Contains(post.Status))
            throw new InvalidOperationException("Post is in an invalid status.");

        if (post.Status is "publishing" or "published")
            throw new InvalidOperationException("Published posts cannot be rescheduled.");

        var scheduled = _timezoneService.ToUtc(request.ScheduledAtUtc);

        if (scheduled <= DateTime.UtcNow.AddMinutes(-1))
            throw new InvalidOperationException("Schedule time must be in the future.");

        if (request.SelectedCaptionIndex.HasValue)
        {
            var idx = request.SelectedCaptionIndex.Value;
            var caption = post.Captions.FirstOrDefault(c => c.VariantIndex == idx);
            if (caption is null)
                throw new InvalidOperationException("Selected caption not found.");

            foreach (var c in post.Captions)
            {
                c.IsSelected = c.VariantIndex == idx;
            }
        }

        if (request.MediaIds?.Count > 0)
        {
            post.PostMedia = request.MediaIds
                .Select(id => new PostMedia { PostId = post.Id, MediaAssetId = id })
                .ToList();
        }

        post.ScheduledAtUtc = scheduled;
        post.Status = "scheduled";

        await _posts.SaveChangesAsync(cancellationToken);
    }
}
