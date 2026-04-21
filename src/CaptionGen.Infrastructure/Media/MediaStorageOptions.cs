namespace CaptionGen.Infrastructure.Media;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    /// <summary>
    /// Physical root path where media files are stored.
    /// </summary>
    public string RootPath { get; set; } = "media";

    /// <summary>
    /// Public base URL used to build accessible links (e.g., http://localhost:5000/media).
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5000/media";
}
