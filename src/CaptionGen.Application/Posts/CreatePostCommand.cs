using MediatR;

namespace CaptionGen.Application.Posts;

public sealed record CreatePostCommand(
    Guid UserId,
    string Description,
    string Platform,
    string Tone,
    string Language,
    string Goal,
    string CaptionLength,
    bool IncludeEmojis,
    bool IncludeCta,
    int HashtagCount,
    string? Audience,
    string? BrandVoice,
    IReadOnlyList<string> ForbiddenWords,
    IReadOnlyList<string> KeywordsToInclude,
    int Count
) : IRequest<CreatePostResponse>;
