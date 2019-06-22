using System;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents information about a build report.
    /// </summary>
    [DataContract]
    public class BuildReportMetadata
    {
        public BuildReportMetadata()
        {
        }

        public BuildReportMetadata(Int32 buildId, String type)
        {
            this.BuildId = buildId;
            this.Type = type;
        }

        /// <summary>
        /// The Id of the build.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 BuildId
        {
            get;
            set;
        }

        /// <summary>
        /// The content of the report.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Content
        {
            get;
            set;
        }

        /// <summary>
        /// The type of the report.
        /// </summary>
        /// <remarks>
        /// See <see cref="ReportTypes" /> for a list of supported report types.
        /// </remarks>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }
    }
}
