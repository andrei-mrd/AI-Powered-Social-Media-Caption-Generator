namespace CaptionGen.Infrastructure.Captions;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    /// <summary>
    /// Base URL for the Python AI service (e.g., http://localhost:8001).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8001";

    /// <summary>
    /// Timeout in seconds for outbound calls to the AI service.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Relative path used by the health check to probe the AI service.
    /// </summary>
    public string HealthPath { get; set; } = "health";
}
