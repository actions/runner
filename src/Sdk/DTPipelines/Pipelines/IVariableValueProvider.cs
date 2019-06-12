using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVariableValueProvider
    {
        String GroupType
        {
            get;
        }

        Boolean ShouldGetValues(IPipelineContext context);

        IList<TaskStep> GetSteps(IPipelineContext context, VariableGroupReference group, IEnumerable<String> keys);

        IDictionary<String, VariableValue> GetValues(VariableGroup group, ServiceEndpoint endpoint, IEnumerable<String> keys, Boolean includeSecrets);
    }
}
