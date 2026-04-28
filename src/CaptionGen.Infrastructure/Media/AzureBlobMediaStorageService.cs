using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CaptionGen.Application.Media;
using Microsoft.Extensions.Options;

namespace CaptionGen.Infrastructure.Media;

public sealed class AzureBlobMediaStorageService : IMediaStorageService
{
    private readonly AzureBlobMediaStorageOptions _options;
    private readonly BlobContainerClient _container;

    public AzureBlobMediaStorageService(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value.AzureBlob;
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("MediaStorage:AzureBlob:ConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.ContainerName))
        {
            throw new InvalidOperationException("MediaStorage:AzureBlob:ContainerName is required.");
        }

        _container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
    }

    public async Task<StoredMedia> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default)
    {
        if (_options.CreateContainerIfNotExists)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        var ext = Path.GetExtension(fileName);
        var safeExt = string.IsNullOrWhiteSpace(ext) ? "" : ext.ToLowerInvariant();
        var name = $"{Guid.NewGuid():N}{safeExt}";
        var blobName = $"{name[..2]}/{name}";
        var blob = _container.GetBlobClient(blobName);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            cancellationToken);

        return new StoredMedia(blobName, BuildPublicUrl(blobName), contentType);
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return;
        }

        await _container
            .GetBlobClient(NormalizeBlobName(storagePath))
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public string BuildPublicUrl(string storagePath)
    {
        var blobName = NormalizeBlobName(storagePath);
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{blobName}";
        }

        return _container.GetBlobClient(blobName).Uri.ToString();
    }

    private static string NormalizeBlobName(string storagePath) =>
        storagePath
            .TrimStart('/', '\\')
            .Replace("\\", "/", StringComparison.Ordinal);
}
