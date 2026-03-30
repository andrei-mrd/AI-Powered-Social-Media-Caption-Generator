using CaptionGen.Application.Captions;
using CaptionGen.Application.Entitlements;
using CaptionGen.Domain.Posts;
using MediatR;

namespace CaptionGen.Application.Posts;

public sealed class CreatePostHandler
    : IRequestHandler<CreatePostCommand, CreatePostResponse>
{
    private readonly IPostRepository _posts;
    private readonly IAiCaptionService _ai;
    private readonly IUsageService _usage;

    public CreatePostHandler(IPostRepository posts, IAiCaptionService ai, IUsageService usage)
    {
        _posts = posts;
        _ai = ai;
        _usage = usage;
    }

    public async Task<CreatePostResponse> Handle(
        CreatePostCommand request,
        CancellationToken cancellationToken)
    {
        // Validation is enforced by the MediatR pipeline (CreatePostCommandValidator).
        // Normalize values here for consistent storage in the domain entity.
        var description = (request.Description ?? string.Empty).Trim();
        var platform = (request.Platform ?? string.Empty).Trim().ToLowerInvariant();
        var tone = (request.Tone ?? string.Empty).Trim().ToLowerInvariant();
        var language = (request.Language ?? string.Empty).Trim().ToLowerInvariant();
        var goal = (request.Goal ?? string.Empty).Trim().ToLowerInvariant();
        var captionLength = (request.CaptionLength ?? "medium").Trim().ToLowerInvariant();
        var includeEmojis = request.IncludeEmojis;
        var includeCta = request.IncludeCta;
        var hashtagCount = request.HashtagCount;
        var audience = request.Audience?.Trim();
        var brandVoice = request.BrandVoice?.Trim();
        var forbiddenWords = (request.ForbiddenWords ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();
        var keywordsToInclude = (request.KeywordsToInclude ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();
        var count = request.Count;

        var options = new CaptionGenerationOptions(
            language,
            goal,
            captionLength,
            includeEmojis,
            includeCta,
            hashtagCount,
            string.IsNullOrWhiteSpace(audience) ? null : audience,
            string.IsNullOrWhiteSpace(brandVoice) ? null : brandVoice,
            forbiddenWords,
            keywordsToInclude,
            Array.Empty<string>());

        var aiResult = await _ai.GenerateAsync(description, platform, tone, count, options, cancellationToken);

        var post = new Post
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Description = description,
            Platform = platform,
            Tone = tone,
            Language = language,
            Goal = goal,
            Status = "draft",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createdAt = DateTime.UtcNow;
        var captions = aiResult.Captions
            .Take(count)
            .Select((variant, idx) => new Caption
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                Post = post,
                VariantIndex = idx,
                Text = variant.Text,
                HashtagsText = string.Join(" ", variant.Hashtags ?? Array.Empty<string>()),
                Hook = variant.Hook,
                Cta = variant.Cta,
                Score = variant.Score,
                CreatedAtUtc = createdAt
            })
            .ToList();

        post.Captions = captions;

        await _posts.AddAsync(post, cancellationToken);
        await _usage.IncrementCaptionsAsync(request.UserId, count, cancellationToken);

        var captionDtos = captions
            .OrderBy(c => c.VariantIndex)
            .Select(c => new CaptionVariantDto(
                c.VariantIndex,
                c.Text,
                (c.HashtagsText ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToArray(),
                c.Hook,
                c.Cta,
                c.Score))
            .ToList();

        return new CreatePostResponse(post.Id, captionDtos, aiResult.Hashtags, aiResult.TraceId);
    }
}
