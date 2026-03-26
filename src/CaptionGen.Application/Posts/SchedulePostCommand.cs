using CaptionGen.Domain.Posts;
using MediatR;

namespace CaptionGen.Application.Posts;

public sealed record SchedulePostCommand(
    Guid UserId,
    Guid PostId,
    DateTime ScheduledAtUtc,
    int? SelectedCaptionIndex,
    IReadOnlyList<Guid> MediaIds) : IRequest;

public sealed record SelectCaptionCommand(
    Guid UserId,
    Guid PostId,
    int VariantIndex) : IRequest;

public sealed class SchedulePostHandler : IRequestHandler<SchedulePostCommand>
{
    private readonly IPostRepository _posts;

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "draft", "scheduled", "publishing", "published", "failed" };

    public SchedulePostHandler(IPostRepository posts)
    {
        _posts = posts;
    }

    public async Task Handle(SchedulePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _posts.GetByIdWithCaptionsAndMediaAsync(request.PostId, request.UserId, cancellationToken);
        if (post is null) throw new InvalidOperationException("Post not found.");

        if (!AllowedStatuses.Contains(post.Status))
            throw new InvalidOperationException("Post is in an invalid status.");

        if (post.Status is "publishing" or "published")
            throw new InvalidOperationException("Published posts cannot be rescheduled.");

        var scheduled = request.ScheduledAtUtc;
        if (scheduled.Kind == DateTimeKind.Unspecified)
        {
            // Treat incoming local wall time as-is but mark it UTC to satisfy the DB.
            scheduled = DateTime.SpecifyKind(scheduled, DateTimeKind.Utc);
        }
        else if (scheduled.Kind == DateTimeKind.Local)
        {
            // Preserve wall time by keeping ticks and marking as UTC (avoid shifting).
            scheduled = new DateTime(scheduled.Ticks, DateTimeKind.Utc);
        }

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
