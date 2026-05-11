using CaptionGen.Domain.Users;
using CaptionGen.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class DbInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ShouldSeedDefaultPlans()
    {
        var services = BuildServices(out var databaseName);
        var configuration = BuildConfiguration(("Database:AutoMigrate", "false"));

        await DbInitializer.InitializeAsync(services, configuration, NullLogger.Instance, CancellationToken.None);

        await using var db = BuildContext(databaseName);
        db.Plans.Select(p => p.Slug).Should().BeEquivalentTo("basic", "freelancer", "influencer", "agency");
    }

    [Fact]
    public async Task InitializeAsync_ShouldAssignBasicPlanToExistingUsersWithoutEntitlements()
    {
        var services = BuildServices(out var databaseName);
        await using (var db = BuildContext(databaseName))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "user@test.local",
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var configuration = BuildConfiguration(("Database:AutoMigrate", "false"));

        await DbInitializer.InitializeAsync(services, configuration, NullLogger.Instance, CancellationToken.None);

        await using var verifyDb = BuildContext(databaseName);
        var basicPlan = await verifyDb.Plans.SingleAsync(p => p.Slug == "basic");
        var entitlement = await verifyDb.UserEntitlements.SingleAsync();
        entitlement.PlanId.Should().Be(basicPlan.Id);
        entitlement.SeatsInUse.Should().Be(1);
        entitlement.ActiveUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_WhenSeedEnabled_ShouldCreateDemoUser()
    {
        var services = BuildServices(out var databaseName);
        var configuration = BuildConfiguration(
            ("Database:AutoMigrate", "false"),
            ("Seed:Enabled", "true"),
            ("Seed:DemoEmail", " Demo@Test.Local "),
            ("Seed:DemoPassword", "secret-password"));

        await DbInitializer.InitializeAsync(services, configuration, NullLogger.Instance, CancellationToken.None);

        await using var db = BuildContext(databaseName);
        var user = await db.Users.SingleAsync();
        user.Email.Should().Be("demo@test.local");
        user.PasswordHash.Should().NotBe("secret-password");
        user.PasswordHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InitializeAsync_WhenDemoCredentialsMissing_ShouldSkipDemoUser()
    {
        var services = BuildServices(out var databaseName);
        var configuration = BuildConfiguration(
            ("Database:AutoMigrate", "false"),
            ("Seed:Enabled", "true"),
            ("Seed:DemoEmail", ""),
            ("Seed:DemoPassword", ""));

        await DbInitializer.InitializeAsync(services, configuration, NullLogger.Instance, CancellationToken.None);

        await using var db = BuildContext(databaseName);
        db.Users.Should().BeEmpty();
    }

    private static ServiceProvider BuildServices(out string databaseName)
    {
        var name = Guid.NewGuid().ToString();
        databaseName = name;
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(name));
        return services.BuildServiceProvider();
    }

    private static AppDbContext BuildContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}
