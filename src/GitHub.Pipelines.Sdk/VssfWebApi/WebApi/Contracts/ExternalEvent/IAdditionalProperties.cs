using System.Collections.Generic;

namespace GitHub.Services.ExternalEvent
{
    public interface IAdditionalProperties
    {
        IDictionary<string, object> AdditionalProperties { get; set; }
    }
}
