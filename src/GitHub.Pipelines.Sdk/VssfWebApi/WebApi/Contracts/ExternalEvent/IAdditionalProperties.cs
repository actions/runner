using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.ExternalEvent
{
    public interface IAdditionalProperties
    {
        IDictionary<string, object> AdditionalProperties { get; set; }
    }
}
