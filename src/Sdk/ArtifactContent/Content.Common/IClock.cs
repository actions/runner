using GitHub.Services.Common;
using System;

namespace GitHub.Services.Content.Common
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }

    public sealed class UtcClock : IClock
    {
        public static readonly IClock Instance = new UtcClock();
        private UtcClock() { }
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }

    public sealed class TimeZoneClock : IClock
    {
        public TimeZoneClock(TimeZoneInfo timeZone)
        {
            ArgumentUtility.CheckForNull(timeZone, nameof(timeZone));
            this.timeZone = timeZone;
        }

        private TimeZoneInfo timeZone;

        public DateTimeOffset Now => DateTimeOffset.UtcNow.ToOffset(timeZone.BaseUtcOffset);
    }

    public static class UtcConversionExtensions
    {
        public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Utc:
                    return new DateTimeOffset(dateTime, TimeSpan.Zero);
                case DateTimeKind.Local:
                    return new DateTimeOffset(dateTime, TimeZoneInfo.Local.BaseUtcOffset);
                default:
                    throw new ArgumentException("DateTime must be UTC.");
            }
        }
    }
}
