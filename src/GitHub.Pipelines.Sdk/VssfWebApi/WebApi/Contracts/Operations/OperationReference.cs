using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Operations
{
    /// <summary>
    /// Reference for an async operation.
    /// </summary>
    [DataContract]
    public class OperationReference
    {
        /// <summary>
        /// Default constructor used for serialization.
        /// </summary>
        public OperationReference()
        {
        }

        /// <summary>
        /// Unique identifier for the operation.
        /// </summary>
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// The current status of the operation.
        /// </summary>
        [DataMember(Order = 1)]
        public OperationStatus Status { get; set; }

        /// <summary>
        /// URL to get the full operation object.
        /// </summary>
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public String Url { get; set; }

        /// <summary>
        /// Unique identifier for the plugin.
        /// </summary>
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public Guid PluginId { get; set; }

        public override String ToString()
        {
            return Url;
        }
    }
}
