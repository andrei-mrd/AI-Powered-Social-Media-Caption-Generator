namespace CaptionGen.Application.Common.Time;

public interface ITimezoneService
{
    DateTime ToUtc(DateTime localValue);
}
