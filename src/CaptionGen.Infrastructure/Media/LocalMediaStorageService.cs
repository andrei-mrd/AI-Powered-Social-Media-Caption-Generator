using CaptionGen.Application.Media;
using Microsoft.Extensions.Options;

namespace CaptionGen.Infrastructure.Media;

public sealed class LocalMediaStorageService : IMediaStorageService
{
    private readonly MediaStorageOptions _options;

    public LocalMediaStorageService(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredMedia> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(_options.RootPath);
        Directory.CreateDirectory(root);

        var ext = Path.GetExtension(fileName);
        var safeExt = string.IsNullOrWhiteSpace(ext) ? "" : ext;
        var name = $"{Guid.NewGuid():N}{safeExt}";
        var relativePath = Path.Combine(name[..2], name);
        var fullPath = Path.Combine(root, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        var publicUrl = BuildPublicUrl(relativePath.Replace("\\", "/"));
        return new StoredMedia(relativePath.Replace("\\", "/"), publicUrl, contentType);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            return Task.CompletedTask;

        var root = Path.GetFullPath(_options.RootPath);
        var fullPath = Path.Combine(root, storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public string BuildPublicUrl(string storagePath)
    {
        var trimmed = storagePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{trimmed.Replace("\\", "/")}";
    }
}
