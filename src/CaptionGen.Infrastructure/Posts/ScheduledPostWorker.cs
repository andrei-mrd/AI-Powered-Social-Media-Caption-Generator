using CaptionGen.Application.Common.Time;
using CaptionGen.Application.Posts;
using CaptionGen.Application.Social;
using CaptionGen.Domain.Posts;
using CaptionGen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CaptionGen.Infrastructure.Posts;

/// <summary>
/// Background worker that dequeues scheduled posts and simulates publishing.
/// </summary>
public sealed class ScheduledPostWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScheduledPostWorker> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly int _batchSize;

    public ScheduledPostWorker(
        IServiceProvider services,
        IOptions<SchedulingOptions> options,
        ILogger<ScheduledPostWorker> logger)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(options.Value.CheckIntervalSeconds);
        _batchSize = options.Value.BatchSize;
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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publishers = scope.ServiceProvider.GetServices<ISocialPublisher>().ToList();

        var due = await repo.AcquireDueScheduledAsync(DateTime.UtcNow, _batchSize, ct);
        if (due.Count == 0) return;

        foreach (var post in due)
        {
            await ProcessPost(post, db, publishers, ct);
        }

        await repo.SaveChangesAsync(ct);
    }

    private async Task ProcessPost(
        Post post,
        AppDbContext db,
        IReadOnlyCollection<ISocialPublisher> publishers,
        CancellationToken ct)
    {
        try
        {
            await PublishSelectedCaption(post, db, publishers, ct);
            post.Status = "published";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish post {PostId}", post.Id);
            post.Status = "failed";
        }
    }

    private async Task PublishSelectedCaption(
        Post post,
        AppDbContext db,
        IReadOnlyCollection<ISocialPublisher> publishers,
        CancellationToken ct)
    {
        var selectedCaption = post.Captions.FirstOrDefault(c => c.IsSelected)
            ?? post.Captions.OrderBy(c => c.VariantIndex).FirstOrDefault();
        if (selectedCaption is null)
        {
            return;
        }

        var account = await db.SocialAccounts
            .FirstOrDefaultAsync(a => a.UserId == post.UserId && a.Platform == post.Platform, ct);
        if (account is null)
        {
            _logger.LogWarning(
                "Post {PostId} scheduled for {Platform} but user has no connected account",
                post.Id, post.Platform);
            return;
        }

        var publisher = publishers.FirstOrDefault(p => p.Platform == post.Platform);
        if (publisher is null)
        {
            _logger.LogWarning("No publisher registered for platform {Platform}", post.Platform);
            return;
        }

        await publisher.PublishAsync(account, selectedCaption.Text, ct);
    }
}
