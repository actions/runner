using System;

namespace GitHub.Services.Common
{
    public static class PerformanceTimerConstants
    {
        public const string Header = "X-VSS-PerfData";
        public const string PerfTimingKey = "PerformanceTimings";

        [Obsolete]
        public const string Aad = "AAD"; // Previous timer, broken into Token and Graph below

        public const string AadToken = "AadToken";
        public const string AadGraph = "AadGraph";
        public const string BlobStorage = "BlobStorage";
        public const string FinalSqlCommand = "FinalSQLCommand";
        public const string Redis = "Redis";
        public const string ServiceBus = "ServiceBus";
        public const string Sql = "SQL";
        public const string SqlReadOnly = "SQLReadOnly";
        public const string SqlRetries = "SQLRetries";
        public const string TableStorage = "TableStorage";
        public const string VssClient = "VssClient";
        public const string DocumentDB = "DocumentDB";
    }
}
