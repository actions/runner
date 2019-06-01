using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVariableGroupStore : IStepProvider
    {
        IList<VariableGroupReference> GetAuthorizedReferences();

        VariableGroup Get(VariableGroupReference queue);

        IVariableValueProvider GetValueProvider(VariableGroupReference queue);

        /// <summary>
        /// Gets the <c>IVariableGroupsResolver</c> used by this store, if any.
        /// </summary>
        IVariableGroupResolver Resolver { get; }
    }
}
