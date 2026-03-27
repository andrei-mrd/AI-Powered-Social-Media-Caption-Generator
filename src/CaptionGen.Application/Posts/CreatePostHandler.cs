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

    private static readonly HashSet<string> AllowedPlatforms =
        new(StringComparer.OrdinalIgnoreCase) { "instagram", "tiktok", "linkedin" };

    private static readonly HashSet<string> AllowedTones =
        new(StringComparer.OrdinalIgnoreCase) { "funny", "professional", "inspirational" };

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
        var description = (request.Description ?? string.Empty).Trim();
        var platform = (request.Platform ?? string.Empty).Trim().ToLowerInvariant();
        var tone = (request.Tone ?? string.Empty).Trim().ToLowerInvariant();
        var language = (request.Language ?? string.Empty).Trim().ToLowerInvariant();
        var goal = (request.Goal ?? string.Empty).Trim();
        var captionLength = (request.CaptionLength ?? "medium").Trim().ToLowerInvariant();
        var includeEmojis = request.IncludeEmojis;
        var includeCta = request.IncludeCta;
        var hashtagCount = request.HashtagCount <= 0 ? 12 : request.HashtagCount;
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

        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Description is required.");

        if (!AllowedPlatforms.Contains(platform))
            throw new InvalidOperationException("Platform must be instagram, tiktok, or linkedin.");

        if (!AllowedTones.Contains(tone))
            throw new InvalidOperationException("Tone must be funny, professional, or inspirational.");

        if (count is < 1 or > 10)
            throw new InvalidOperationException("Count must be between 1 and 10.");

        if (string.IsNullOrWhiteSpace(language))
            throw new InvalidOperationException("Language is required.");

        if (string.IsNullOrWhiteSpace(goal))
            throw new InvalidOperationException("Goal is required.");

        if (captionLength is not ("short" or "medium" or "long"))
            throw new InvalidOperationException("Caption length must be short, medium, or long.");

        if (hashtagCount is < 5 or > 20)
            throw new InvalidOperationException("Hashtag count must be between 5 and 20.");

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
