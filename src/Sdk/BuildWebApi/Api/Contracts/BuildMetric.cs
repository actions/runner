using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents metadata about builds in the system.
    /// </summary>
    [DataContract]
    public class BuildMetric : BaseSecuredObject
    {
        public BuildMetric()
        {
        }

        internal BuildMetric(
            ISecuredObject securedObject)
            :base(securedObject)
        {
        }

        /// <summary>
        /// The name of the metric.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The scope.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Scope
        {
            get;
            set;
        }

        /// <summary>
        /// The value.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Int32 IntValue
        {
            get;
            set;
        }

        /// <summary>
        /// The date for the scope.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? Date
        {
            get;
            set;
        }
    }
}
