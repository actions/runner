using GitHub.Services.WebApi;
using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Operations
{
    /// <summary>
    /// Contains information about the progress or result of an async operation.
    /// </summary>
    [DataContract]
    public class Operation : OperationReference
    {
        // For serialization
        public Operation()
        {
        }

        /// <summary>
        /// Initializes the Operation with the values from the OperationReference.
        /// </summary>
        /// <param name="operationReference">Reference upon which to base this Operation.</param>
        /// <remarks>Will initialize the Links and add the "self" reference.</remarks>
        public Operation(OperationReference operationReference)
        {
            Id = operationReference.Id;
            PluginId = operationReference.PluginId;
            Status = operationReference.Status;
            Url = operationReference.Url;

            Links = new ReferenceLinks();
            Links.AddLink("self", Url);
        }

        /// <summary>
        /// Links to other related objects.
        /// </summary>
        [DataMember(Name = "_links", Order = 6, EmitDefaultValue = false)]
        public ReferenceLinks Links { get; set; }

        /// <summary>
        /// Result message for an operation.
        /// </summary>
        [DataMember(Order = 7, EmitDefaultValue = false)]
        public String ResultMessage { get; set; }

        /// <summary>
        /// Detailed messaged about the status of an operation.
        /// </summary>
        [DataMember(Order = 8, EmitDefaultValue = false)]
        public String DetailedMessage { get; set; }

        /// <summary>
        /// URL to the operation result.
        /// </summary>
        [DataMember(Order = 9, EmitDefaultValue = false)]
        public OperationResultReference ResultUrl { get; set; }

        /// <summary>
        /// Operation completed with success or failure
        /// </summary>
        public Boolean Completed =>
            Status == OperationStatus.Succeeded ||
            Status == OperationStatus.Failed ||
            Status == OperationStatus.Cancelled;

        public override String ToString()
        {
            return Status.ToString();
        }
    }
}
