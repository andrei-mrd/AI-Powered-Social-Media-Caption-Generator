using System.Collections.Generic;

namespace CaptionGen.Application.Posts;

public sealed record CreatePostResponse(
    Guid Id,
    IReadOnlyList<string> Captions,
    IReadOnlyList<string> Hashtags);
