using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class MetricAggregationTypes
    {
        public const String Hourly = "Hourly";
        public const String Daily = "Daily";
    }

    [Obsolete("Use MetricAggregationTypes instead.")]
    public static class WellKnownMetricAggregationTypes
    {
        public const String Hourly = MetricAggregationTypes.Hourly;
        public const String Daily = MetricAggregationTypes.Daily;
    }
}
