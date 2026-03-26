using BCryptNet = BCrypt.Net.BCrypt;
using CaptionGen.Domain.Users;
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
            logger.LogInformation("Seed skipped: demo user already exists ({Email}).", demoEmail);
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

        logger.LogInformation("Seeded demo user {Email}.", demoEmail);
    }
}
