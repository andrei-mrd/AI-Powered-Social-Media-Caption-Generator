using CaptionGen.Application.Common.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CaptionGen.Infrastructure.Common;

public sealed class TimezoneService : ITimezoneService
{
    private readonly SchedulingOptions _options;
    private readonly ILogger<TimezoneService> _logger;

    public TimezoneService(IOptions<SchedulingOptions> options, ILogger<TimezoneService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public DateTime ToUtc(DateTime localValue)
    {
        var tzId = _options.LocalTimezone;
        var tz = ResolveTimezone(tzId);

        if (localValue.Kind == DateTimeKind.Utc)
            return localValue;

        var local = localValue.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(localValue, DateTimeKind.Unspecified)
            : localValue;

        if (tz is not null)
        {
            return TimeZoneInfo.ConvertTimeToUtc(local, tz);
        }

        // Fallback: assume provided value already represents local time; preserve wall time ticks.
        return new DateTime(local.Ticks, DateTimeKind.Utc);
    }

    private TimeZoneInfo? ResolveTimezone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var candidates = new[] { id, "Europe/Bucharest", "GTB Standard Time" };
        foreach (var candidate in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogWarning(ex, "Timezone {Timezone} not found", candidate);
            }
            catch (InvalidTimeZoneException ex)
            {
                _logger.LogWarning(ex, "Timezone {Timezone} is invalid", candidate);
            }
        }

        return null;
    }
}
