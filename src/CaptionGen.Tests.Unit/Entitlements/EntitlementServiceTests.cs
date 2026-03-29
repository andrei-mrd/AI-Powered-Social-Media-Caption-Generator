using CaptionGen.Application.Entitlements;
using CaptionGen.Domain.Entitlements;
using CaptionGen.Infrastructure.Entitlements;
using CaptionGen.Infrastructure.Persistence;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaptionGen.Tests.Unit.Entitlements;

public sealed class EntitlementServiceTests
{
    [Fact]
    public async Task GetForUserAsync_ShouldFallbackToBasicPlan()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        db.Plans.Add(new Plan
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
        });
        await db.SaveChangesAsync();

        var usage = new Mock<IUsageService>(MockBehavior.Loose);
        usage.Setup(x => x.GetUsageAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageSnapshot(DateTime.UtcNow, 0, 0));

        var sut = new EntitlementService(db, usage.Object, NullLogger<EntitlementService>.Instance);
        var ent = await sut.GetForUserAsync(Guid.NewGuid(), CancellationToken.None);

        ent.PlanSlug.Should().Be("basic");
        ent.CaptionGenerationsPerMonth.Should().Be(30);
    }

    [Fact]
    public async Task GetForUserAsync_ShouldReturnUserPlan()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using var db = new AppDbContext(options);
        var plan = new Plan
        {
            Id = planId,
            Slug = "agency",
            Name = "Agency",
            CaptionGenerationsPerMonth = 1000,
            MediaAssetsLimit = 1000,
            SeatsIncluded = 5,
            SchedulingEnabled = true,
            AiImproveEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Plans.Add(plan);
        db.UserEntitlements.Add(new UserEntitlement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            Plan = plan,
            SeatsInUse = 1,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var usage = new Mock<IUsageService>(MockBehavior.Loose);
        usage.Setup(x => x.GetUsageAsync(userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageSnapshot(DateTime.UtcNow, 0, 0));

        var sut = new EntitlementService(db, usage.Object, NullLogger<EntitlementService>.Instance);
        var ent = await sut.GetForUserAsync(userId, CancellationToken.None);

        ent.PlanSlug.Should().Be("agency");
        ent.SeatsIncluded.Should().Be(5);
    }
}
