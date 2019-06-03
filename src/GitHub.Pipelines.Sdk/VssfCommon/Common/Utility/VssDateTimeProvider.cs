using System;
using System.Globalization;

namespace GitHub.Services.Common
{
    public interface IVssDateTimeProvider
    {
        string Name { get; }

        DateTime Now { get; }

        DateTime UtcNow { get; }

        DateTime Convert(DateTime time);
    }

    public class VssDateTimeProvider : IVssDateTimeProvider
    {
        public static readonly IVssDateTimeProvider DefaultProvider = new DefaultDateTimeProvider();
        
        private class DefaultDateTimeProvider : IVssDateTimeProvider
        {
            public string Name => nameof(DefaultDateTimeProvider);

            public DateTime Now => DateTime.Now;

            public DateTime UtcNow => DateTime.UtcNow;

            public DateTime Convert(DateTime time) => time;
        }

        public string Name { get; }

        public DateTime Now => Transform != null ? Transform.Now : DateTime.Now;

        public DateTime UtcNow => Transform != null ? Transform.UtcNow : DateTime.UtcNow;

        public DateTime Convert(DateTime time) => Transform != null ? Transform.Convert(time) : time;

        /// <summary>
        /// Sets a transform such that this provider will differ from the actual time by a fixed TimeSpan offset.
        /// Throws an exception if there is already an active transform on the provider (concurrent transforms are not allowed).
        /// Callers MUST dispose of the result, such as by a <code>using</code> statement, to scope the lifetime of the transform.
        /// </summary>
        public IDisposable SetOffset(TimeSpan offset) => SetTransform(new FixedOffsetDateTimeTransform(this, offset));

        /// <summary>
        /// Sets a transform such that this provider will always return the given time as the current time
        /// and will shift other datetimes according to the diffence between the current time and the fixed target time.
        /// Throws an exception if there is already an active transform on the provider (concurrent transforms are not allowed).
        /// Callers MUST dispose of the result, such as by a <code>using</code> statement, to scope the lifetime of the transform.
        /// </summary>
        public IDisposable SetNow(DateTime now) => SetTransform(new FixedNowDateTimeTransform(this, now));

        public VssDateTimeProvider(string name)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(name, nameof(name));

            Name = name;
        }

        private IDisposable SetTransform(DateTimeTransform transform)
        {
            if (Transform != null)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                                  "Cannot create more than one concurrent date time transform on the same date time provider. Callers MUST dispose the current transform before setting the new transform, ideally by wrapping in separate using statements.\nProvider: {0}, Current transform: {1}, New transform: {2}",
                                  Name, Transform, transform));
            }

            Transform = transform;

            return transform;
        }

        private DateTimeTransform Transform { get; set; }

        private abstract class DateTimeTransform : IDisposable
        {
            public abstract DateTime Now { get; }

            public abstract DateTime UtcNow { get; }

            public abstract DateTime Convert(DateTime time);

            public DateTimeTransform(VssDateTimeProvider provider)
            {
                Provider = provider;
            }

            public void Dispose()
            {
                if (Provider.Transform == this)
                {
                    Provider.Transform = null;
                }
            }

            private VssDateTimeProvider Provider { get; }
        }

        private class FixedOffsetDateTimeTransform : DateTimeTransform
        {
            public override DateTime Now => DateTime.Now + Offset;

            public override DateTime UtcNow => DateTime.UtcNow + Offset;

            public override DateTime Convert(DateTime time) => time + Offset;

            public FixedOffsetDateTimeTransform(VssDateTimeProvider provider, TimeSpan offset)
                : base(provider)
            {
                Offset = offset;
            }

            public override string ToString()
            {
                return nameof(FixedOffsetDateTimeTransform) + " offset: " + Offset;
            }

            private TimeSpan Offset { get; }
        }

        private class FixedNowDateTimeTransform : DateTimeTransform
        {
            public override DateTime Now { get; }

            public override DateTime UtcNow => Now.ToUniversalTime();

            public override DateTime Convert(DateTime time) => time + (Now - DateTime.Now);

            public FixedNowDateTimeTransform(VssDateTimeProvider provider, DateTime now)
                : base(provider)
            {
                Now = now;
            }

            public override string ToString()
            {
                return nameof(FixedNowDateTimeTransform) + " now: " + Now;
            }
        }
    }
}
