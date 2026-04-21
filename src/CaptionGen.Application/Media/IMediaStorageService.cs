namespace CaptionGen.Application.Media;

public sealed record StoredMedia(string StoragePath, string PublicUrl, string MediaType);

public interface IMediaStorageService
{
    Task<StoredMedia> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    string BuildPublicUrl(string storagePath);
}
