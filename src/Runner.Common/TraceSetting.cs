using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace GitHub.Runner.Common
{
    [DataContract]
    public class TraceSetting
    {
        public TraceSetting()
        {
            DefaultTraceLevel = TraceLevel.Info;
#if DEBUG
            DefaultTraceLevel = TraceLevel.Verbose;
#endif            
            string actionsRunnerTrace = Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_TRACE");
            if (!string.IsNullOrEmpty(actionsRunnerTrace))
            {
                DefaultTraceLevel = TraceLevel.Verbose;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TraceLevel DefaultTraceLevel
        {
            get;
            set;
        }

        public Dictionary<String, TraceLevel> DetailTraceSetting
        {
            get
            {
                if (m_detailTraceSetting == null)
                {
                    m_detailTraceSetting = new Dictionary<String, TraceLevel>(StringComparer.OrdinalIgnoreCase);
                }
                return m_detailTraceSetting;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "DetailTraceSetting")]
        private Dictionary<String, TraceLevel> m_detailTraceSetting;
    }

    [DataContract]
    public enum TraceLevel
    {
        [EnumMember]
        Off = 0,

        [EnumMember]
        Critical = 1,

        [EnumMember]
        Error = 2,

        [EnumMember]
        Warning = 3,

        [EnumMember]
        Info = 4,

        [EnumMember]
        Verbose = 5,
    }

    public static class TraceLevelExtensions
    {
        public static SourceLevels ToSourceLevels(this TraceLevel traceLevel)
        {
            switch (traceLevel)
            {
                case TraceLevel.Off:
                    return SourceLevels.Off;
                case TraceLevel.Critical:
                    return SourceLevels.Critical;
                case TraceLevel.Error:
                    return SourceLevels.Error;
                case TraceLevel.Warning:
                    return SourceLevels.Warning;
                case TraceLevel.Info:
                    return SourceLevels.Information;
                case TraceLevel.Verbose:
                    return SourceLevels.Verbose;
                default:
                    return SourceLevels.Information;
            }
        }
    }
}
