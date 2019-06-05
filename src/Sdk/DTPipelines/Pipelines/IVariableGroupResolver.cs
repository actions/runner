using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism of resolving an <c>VariableGroupReference</c> to a <c>VariableGroup</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVariableGroupResolver
    {
        /// <summary>
        /// Attempts to resolve variable group references to <c>VariableGroup</c> instances.
        /// </summary>
        /// <param name="reference">The variable groups which should be resolved</param>
        /// <returns>The resolved variable groups</returns>
        IList<VariableGroup> Resolve(ICollection<VariableGroupReference> references);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IVariableGroupResolverExtensions
    {
        public static VariableGroup Resolve(
            this IVariableGroupResolver resolver,
            VariableGroupReference reference)
        {
            return resolver.Resolve(new[] { reference }).FirstOrDefault();
        }
    }
}
