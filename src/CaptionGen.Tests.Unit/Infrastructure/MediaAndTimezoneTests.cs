using CaptionGen.Application.Common.Time;
using CaptionGen.Infrastructure.Common;
using CaptionGen.Infrastructure.Media;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CaptionGen.Tests.Unit.Infrastructure;

public sealed class MediaAndTimezoneTests
{
    [Fact]
    public void AzureBlobMediaStorageService_WhenConnectionStringMissing_ShouldThrow()
    {
        var options = Options.Create(new MediaStorageOptions
        {
            AzureBlob = new AzureBlobMediaStorageOptions
            {
                ContainerName = "media"
            }
        });

        var act = () => new AzureBlobMediaStorageService(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void AzureBlobMediaStorageService_WhenContainerNameMissing_ShouldThrow()
    {
        var options = Options.Create(new MediaStorageOptions
        {
            AzureBlob = new AzureBlobMediaStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = ""
            }
        });

        var act = () => new AzureBlobMediaStorageService(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ContainerName*");
    }

    [Fact]
    public void TimezoneService_WhenInputAlreadyUtc_ShouldReturnSameValue()
    {
        var sut = BuildTimezoneService("UTC");
        var value = new DateTime(2026, 1, 1, 12, 30, 0, DateTimeKind.Utc);

        var result = sut.ToUtc(value);

        result.Should().Be(value);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void TimezoneService_WhenTimezoneMissing_ShouldPreserveWallClockTicksAsUtc()
    {
        var sut = BuildTimezoneService(null);
        var value = new DateTime(2026, 1, 1, 12, 30, 0, DateTimeKind.Unspecified);

        var result = sut.ToUtc(value);

        result.Ticks.Should().Be(value.Ticks);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void TimezoneService_WhenTimezoneIsConfigured_ShouldConvertToUtc()
    {
        var sut = BuildTimezoneService("UTC");
        var value = new DateTime(2026, 1, 1, 12, 30, 0, DateTimeKind.Unspecified);

        var result = sut.ToUtc(value);

        result.Should().Be(new DateTime(2026, 1, 1, 12, 30, 0, DateTimeKind.Utc));
    }

    private static TimezoneService BuildTimezoneService(string? timezone) =>
        new(
            Options.Create(new SchedulingOptions { LocalTimezone = timezone ?? "" }),
            NullLogger<TimezoneService>.Instance);
}
