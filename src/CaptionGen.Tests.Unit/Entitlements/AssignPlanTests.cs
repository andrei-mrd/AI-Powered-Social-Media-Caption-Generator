using CaptionGen.Application.Entitlements;
using CaptionGen.Domain.Entitlements;
using CaptionGen.Infrastructure.Entitlements;
using CaptionGen.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CaptionGen.Tests.Unit.Entitlements;

public sealed class AssignPlanTests
{
    private static AppDbContext MakeDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Plan MakePlan(string slug) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Name = slug,
        CaptionGenerationsPerMonth = 100,
        MediaAssetsLimit = 50,
        SeatsIncluded = 1,
        SchedulingEnabled = true,
        AiImproveEnabled = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task AssignPlanAsync_ShouldCreateEntitlementWhenNoneExists()
    {
        await using var db = MakeDb();
        db.Plans.Add(MakePlan("basic"));
        await db.SaveChangesAsync();

        var usage = new Mock<IUsageService>(MockBehavior.Loose);
        var sut = new EntitlementService(db, usage.Object, NullLogger<EntitlementService>.Instance);
        var userId = Guid.NewGuid();

        await sut.AssignPlanAsync(userId, "basic");

        var entitlement = await db.UserEntitlements.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == userId);
        entitlement.Should().NotBeNull();
        entitlement!.PlanId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssignPlanAsync_ShouldUpdateExistingEntitlement()
    {
        await using var db = MakeDb();
        var basicPlan = MakePlan("basic");
        var proPlan = MakePlan("freelancer");
        db.Plans.AddRange(basicPlan, proPlan);

        var userId = Guid.NewGuid();
        db.UserEntitlements.Add(new UserEntitlement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = basicPlan.Id,
            SeatsInUse = 1,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var usage = new Mock<IUsageService>(MockBehavior.Loose);
        var sut = new EntitlementService(db, usage.Object, NullLogger<EntitlementService>.Instance);

        await sut.AssignPlanAsync(userId, "freelancer");

        var entitlement = await db.UserEntitlements.FirstAsync(e => e.UserId == userId);
        entitlement.PlanId.Should().Be(proPlan.Id);
    }

    [Fact]
    public async Task AssignPlanAsync_WhenPlanNotFound_ShouldThrow()
    {
        await using var db = MakeDb();
        var usage = new Mock<IUsageService>(MockBehavior.Loose);
        var sut = new EntitlementService(db, usage.Object, NullLogger<EntitlementService>.Instance);

        var act = () => sut.AssignPlanAsync(Guid.NewGuid(), "nonexistent");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task AssignPlanAsync_WhenSlugEmpty_ShouldThrow()
    {
        await using var db = MakeDb();
        var usage = new Mock<IUsageService>(MockBehavior.Loose);
        var sut = new EntitlementService(db, usage.Object, NullLogger<EntitlementService>.Instance);

        var act = () => sut.AssignPlanAsync(Guid.NewGuid(), "  ");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*required*");
    }
}
