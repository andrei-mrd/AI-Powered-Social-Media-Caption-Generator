using CaptionGen.Infrastructure.Entitlements;
using CaptionGen.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class UsageServiceTests
{
    private static AppDbContext MakeDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetUsageAsync_WhenNoRecord_ShouldReturnZeros()
    {
        await using var db = MakeDb();
        var sut = new UsageService(db);

        var result = await sut.GetUsageAsync(Guid.NewGuid(), DateTime.UtcNow);

        result.CaptionsUsed.Should().Be(0);
        result.MediaUsed.Should().Be(0);
    }

    [Fact]
    public async Task IncrementCaptionsAsync_ShouldPersistCount()
    {
        await using var db = MakeDb();
        var sut = new UsageService(db);
        var userId = Guid.NewGuid();

        await sut.IncrementCaptionsAsync(userId, 3);
        var result = await sut.GetUsageAsync(userId, DateTime.UtcNow);

        result.CaptionsUsed.Should().Be(3);
    }

    [Fact]
    public async Task IncrementCaptionsAsync_CalledTwice_ShouldAccumulate()
    {
        await using var db = MakeDb();
        var sut = new UsageService(db);
        var userId = Guid.NewGuid();

        await sut.IncrementCaptionsAsync(userId, 2);
        await sut.IncrementCaptionsAsync(userId, 5);
        var result = await sut.GetUsageAsync(userId, DateTime.UtcNow);

        result.CaptionsUsed.Should().Be(7);
    }

    [Fact]
    public async Task IncrementMediaAsync_ShouldPersistCount()
    {
        await using var db = MakeDb();
        var sut = new UsageService(db);
        var userId = Guid.NewGuid();

        await sut.IncrementMediaAsync(userId, 4);
        var result = await sut.GetUsageAsync(userId, DateTime.UtcNow);

        result.MediaUsed.Should().Be(4);
    }

    [Fact]
    public async Task IncrementCaptionsAsync_WithZeroOrNegative_ShouldNotChangeRecord()
    {
        await using var db = MakeDb();
        var sut = new UsageService(db);
        var userId = Guid.NewGuid();

        await sut.IncrementCaptionsAsync(userId, 0);
        await sut.IncrementCaptionsAsync(userId, -1);
        var result = await sut.GetUsageAsync(userId, DateTime.UtcNow);

        result.CaptionsUsed.Should().Be(0);
    }

    [Fact]
    public async Task GetUsageAsync_ShouldBeScopedToMonthPeriod()
    {
        await using var db = MakeDb();
        var sut = new UsageService(db);
        var userId = Guid.NewGuid();

        await sut.IncrementCaptionsAsync(userId, 10);

        // Query for a different month — should return zero
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var result = await sut.GetUsageAsync(userId, lastMonth);

        result.CaptionsUsed.Should().Be(0);
    }

    [Fact]
    public async Task GetUsageAsync_ShouldBeIsolatedPerUser()
    {
        await using var db = MakeDb();
        var sut = new UsageService(db);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await sut.IncrementCaptionsAsync(user1, 5);
        var result = await sut.GetUsageAsync(user2, DateTime.UtcNow);

        result.CaptionsUsed.Should().Be(0);
    }
}
