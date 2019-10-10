using System;

namespace GitHub.Build.WebApi
{
    public static class BuildIssueKeys
    {
        public const String CodeCategory = "code";
        public const String SourcePath = "sourcePath";
        public const String LineNumber = "lineNumber";
        public const String Message = "message";
    }

    [Obsolete("Use BuildIssueKeys instead.")]
    public static class WellKnownBuildKeys
    {
        public const String BuildIssueCodeCategory = BuildIssueKeys.CodeCategory;
        public const String BuildIssueFileKey = BuildIssueKeys.SourcePath;
        public const String BuildIssueLineNumberKey = BuildIssueKeys.LineNumber;
        public const String BuildIssueMessageKey = BuildIssueKeys.Message;
    }
}
