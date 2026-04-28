namespace CaptionGen.Infrastructure.Media;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    /// <summary>
    /// Storage provider to use: Local or AzureBlob.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Physical root path where media files are stored.
    /// </summary>
    public string RootPath { get; set; } = "media";

    /// <summary>
    /// Public base URL used to build accessible links (e.g., http://localhost:5000/media).
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5000/media";

    public AzureBlobMediaStorageOptions AzureBlob { get; set; } = new();
}

public sealed class AzureBlobMediaStorageOptions
{
    public string ConnectionString { get; set; } = "";
    public string ContainerName { get; set; } = "captiongen-media";
    public string PublicBaseUrl { get; set; } = "";
    public bool CreateContainerIfNotExists { get; set; } = true;
}
