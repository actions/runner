using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace GitHub.Services.Operations
{
    [DataContract]
    public class OperationResultReference
    {
        /// <summary>
        /// URL to the operation result.
        /// </summary>
        [DataMember(Order = 10, EmitDefaultValue = false)]
        public string ResultUrl { get; set; }

        public override string ToString()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                @"OperationResultReference
[
    ResultUrl:                {0}
]",
                ResultUrl);
        }
    }
}
