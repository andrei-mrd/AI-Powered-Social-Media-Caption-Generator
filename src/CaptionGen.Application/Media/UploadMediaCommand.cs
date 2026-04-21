using MediatR;

namespace CaptionGen.Application.Media;

public sealed record MediaAssetDto(
    Guid Id,
    string Type,
    string Url,
    DateTime CreatedAtUtc);

public sealed record UploadMediaCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long Length,
    Stream Content) : IRequest<MediaAssetDto>;

public sealed record ListMediaQuery(Guid UserId) : IRequest<IReadOnlyList<MediaAssetDto>>;

public sealed record DeleteMediaCommand(Guid MediaId, Guid UserId) : IRequest;
