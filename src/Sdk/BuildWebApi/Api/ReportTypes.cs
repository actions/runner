using System;

namespace GitHub.Build.WebApi
{
    public static class ReportTypes
    {
        public const String Html = "Html";
    }

    [Obsolete("Use ReportTypes instead.")]
    public static class WellKnownReportTypes
    {
        public const String Html = ReportTypes.Html;
    }
}
