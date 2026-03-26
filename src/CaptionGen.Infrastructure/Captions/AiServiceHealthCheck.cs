using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CaptionGen.Infrastructure.Captions;

/// <summary>
/// Lightweight probe to validate the AI service is reachable and healthy.
/// </summary>
public sealed class AiServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiServiceOptions _options;
    private readonly ILogger<AiServiceHealthCheck> _logger;

    public AiServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<AiServiceOptions> options,
        ILogger<AiServiceHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("AiService.Health");
        try
        {
            var response = await client.GetAsync(_options.HealthPath, cancellationToken);
            if (response.StatusCode is HttpStatusCode.OK)
            {
                return HealthCheckResult.Healthy("AI service reachable");
            }

            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI health degraded: {Status} {Detail}", response.StatusCode, detail);
            return HealthCheckResult.Degraded(
                $"AI service responded with {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI health check failed");
            return HealthCheckResult.Unhealthy("AI service unreachable", ex);
        }
    }
}
