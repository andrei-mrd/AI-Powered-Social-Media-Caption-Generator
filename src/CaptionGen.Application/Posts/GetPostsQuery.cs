using System.Collections.Generic;
using MediatR;

namespace CaptionGen.Application.Posts;

public sealed record GetPostsQuery(Guid UserId) : IRequest<IReadOnlyList<PostDto>>;

public sealed record PostDto(
    Guid Id,
    string Platform,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<CaptionDto> Captions);

public sealed record CaptionDto(int VariantIndex, string Text);
