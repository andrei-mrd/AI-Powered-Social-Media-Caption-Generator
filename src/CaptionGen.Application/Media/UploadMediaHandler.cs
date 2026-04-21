using CaptionGen.Application.Entitlements;
using CaptionGen.Domain.Posts;
using MediatR;

namespace CaptionGen.Application.Media;

public sealed class UploadMediaHandler : IRequestHandler<UploadMediaCommand, MediaAssetDto>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "video/mp4",
        "video/quicktime"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    private readonly IMediaAssetRepository _repo;
    private readonly IMediaStorageService _storage;
    private readonly IUsageService _usage;

    public UploadMediaHandler(IMediaAssetRepository repo, IMediaStorageService storage, IUsageService usage)
    {
        _repo = repo;
        _storage = storage;
        _usage = usage;
    }

    public async Task<MediaAssetDto> Handle(UploadMediaCommand request, CancellationToken cancellationToken)
    {
        Validate(request);

        var stored = await _storage.SaveAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            request.Length,
            cancellationToken);

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Type = NormalizeType(request.ContentType),
            StoragePath = stored.StoragePath,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(asset, cancellationToken);
        await _usage.IncrementMediaAsync(request.UserId, 1, cancellationToken);

        return new MediaAssetDto(asset.Id, asset.Type, stored.PublicUrl, asset.CreatedAtUtc);
    }

    private static void Validate(UploadMediaCommand request)
    {
        if (request.Length <= 0)
            throw new InvalidOperationException("File is empty.");

        if (request.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"File too large. Limit is {MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (string.IsNullOrWhiteSpace(request.ContentType) || !AllowedContentTypes.Contains(request.ContentType))
            throw new InvalidOperationException("Unsupported media type. Allowed: jpeg, png, webp, mp4.");

        var ext = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            throw new InvalidOperationException("Filename must have an extension.");
    }

    private static string NormalizeType(string contentType) =>
        contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video" : "image";
}
