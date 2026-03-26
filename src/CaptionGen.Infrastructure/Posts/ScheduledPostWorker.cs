using CaptionGen.Application.Posts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CaptionGen.Infrastructure.Posts;

/// <summary>
/// Background worker that dequeues scheduled posts and simulates publishing.
/// </summary>
public sealed class ScheduledPostWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScheduledPostWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 10;

    public ScheduledPostWorker(IServiceProvider services, ILogger<ScheduledPostWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDuePosts(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScheduledPostWorker cycle failed");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessDuePosts(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPostRepository>();

        var due = await repo.AcquireDueScheduledAsync(DateTime.UtcNow, BatchSize, ct);
        if (due.Count == 0) return;

        foreach (var post in due)
        {
            try
            {
                // Placeholder publisher: simulate latency then mark published.
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                post.Status = "published";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish post {PostId}", post.Id);
                post.Status = "failed";
            }
        }

        await repo.SaveChangesAsync(ct);
    }
}
