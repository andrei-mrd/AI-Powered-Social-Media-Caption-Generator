using BCryptNet = BCrypt.Net.BCrypt;
using CaptionGen.Domain.Users;
using CaptionGen.Domain.Entitlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CaptionGen.Infrastructure.Persistence;

/// <summary>
/// Handles database migration and optional local/demo seeding.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, ILogger logger, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var autoMigrate = config.GetValue("Database:AutoMigrate", true);
        if (autoMigrate)
        {
            await db.Database.MigrateAsync(ct);
        }

        await SeedPlansAsync(db, logger, ct);
        await EnsureUserEntitlementsAsync(db, logger, ct);

        var seedEnabled = config.GetValue("Seed:Enabled", false);
        if (!seedEnabled)
        {
            return;
        }

        var demoEmail = (config["Seed:DemoEmail"] ?? string.Empty).Trim().ToLowerInvariant();
        var demoPassword = (config["Seed:DemoPassword"] ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(demoEmail) || string.IsNullOrWhiteSpace(demoPassword))
        {
            logger.LogWarning("Seeding enabled but demo credentials are missing. Skipping seed.");
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == demoEmail, ct))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Seed skipped: demo user already exists ({Email}).", demoEmail);
            }
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = demoEmail,
            PasswordHash = BCryptNet.HashPassword(demoPassword),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded demo user {Email}.", demoEmail);
        }
    }

    private static async Task SeedPlansAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var existingSlugs = await db.Plans.Select(p => p.Slug).ToListAsync(ct);

        var plans = new[]
        {
            new Plan
            {
                Id = Guid.NewGuid(),
                Slug = "basic",
                Name = "Basic",
                CaptionGenerationsPerMonth = 30,
                MediaAssetsLimit = 20,
                SeatsIncluded = 1,
                SchedulingEnabled = true,
                AiImproveEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Plan
            {
                Id = Guid.NewGuid(),
                Slug = "influencer",
                Name = "Influencer",
                CaptionGenerationsPerMonth = 200,
                MediaAssetsLimit = 200,
                SeatsIncluded = 1,
                SchedulingEnabled = true,
                AiImproveEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Plan
            {
                Id = Guid.NewGuid(),
                Slug = "agency",
                Name = "Agency",
                CaptionGenerationsPerMonth = 1000,
                MediaAssetsLimit = 1000,
                SeatsIncluded = 5,
                SchedulingEnabled = true,
                AiImproveEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Plan
            {
                Id = Guid.NewGuid(),
                Slug = "freelancer",
                Name = "Freelancer",
                CaptionGenerationsPerMonth = 150,
                MediaAssetsLimit = 150,
                SeatsIncluded = 1,
                SchedulingEnabled = true,
                AiImproveEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var toInsert = plans.Where(p => !existingSlugs.Contains(p.Slug)).ToList();
        if (toInsert.Count > 0)
        {
            db.Plans.AddRange(toInsert);
            await db.SaveChangesAsync(ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Seeded plans: {Slugs}", string.Join(", ", toInsert.Select(p => p.Slug)));
            }
        }
    }

    private static async Task EnsureUserEntitlementsAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var basicPlan = await db.Plans.FirstOrDefaultAsync(p => p.Slug == "basic", ct)
                        ?? await db.Plans.FirstOrDefaultAsync(p => p.Slug == "free", ct);

        if (basicPlan is null)
        {
            logger.LogWarning("No basic/free plan found; skipping entitlement backfill.");
            return;
        }

        var freePlan = await db.Plans.FirstOrDefaultAsync(p => p.Slug == "free", ct);

        var usersWithoutEntitlement = await db.Users
            .Where(u => !db.UserEntitlements.Any(e => e.UserId == u.Id))
            .ToListAsync(ct);

        if (usersWithoutEntitlement.Count > 0)
        {
            foreach (var user in usersWithoutEntitlement)
            {
                db.UserEntitlements.Add(new UserEntitlement
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PlanId = basicPlan.Id,
                    ActiveUntilUtc = null,
                    SeatsInUse = 1,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Assigned basic plan to {Count} users without entitlements.", usersWithoutEntitlement.Count);
            }
        }

        if (freePlan is not null && basicPlan.Id != freePlan.Id)
        {
            var updated = await db.UserEntitlements
                .Where(e => e.PlanId == freePlan.Id)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.PlanId, basicPlan.Id), ct);

            if (updated > 0 && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated {Count} user entitlements from free to basic.", updated);
            }
        }
    }
}
