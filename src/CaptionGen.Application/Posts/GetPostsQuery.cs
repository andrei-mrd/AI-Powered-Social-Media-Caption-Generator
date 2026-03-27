using System.Collections.Generic;
using MediatR;

namespace CaptionGen.Application.Posts;

public sealed record GetPostsQuery(Guid UserId) : IRequest<IReadOnlyList<PostDto>>;

public sealed record PostDto(
    Guid Id,
    string Platform,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ScheduledAtUtc,
    string? SelectedCaption,
    IReadOnlyList<string> SelectedHashtags,
    int? SelectedScore,
    IReadOnlyList<CaptionDto> Captions,
    IReadOnlyList<MediaDto> Media);

public sealed record CaptionDto(
    int VariantIndex,
    string Text,
    bool IsSelected,
    IReadOnlyList<string> Hashtags,
    string? Hook,
    string? Cta,
    int? Score);

public sealed record MediaDto(Guid Id, string Type, string Url, DateTime CreatedAtUtc);
