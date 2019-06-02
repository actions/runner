using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TaskTemplateStore : ITaskTemplateStore
    {
        public TaskTemplateStore(IList<ITaskTemplateResolver> resolvers)
        {
            m_resolvers = new List<ITaskTemplateResolver>(resolvers ?? Enumerable.Empty<ITaskTemplateResolver>());
        }

        public void AddProvider(ITaskTemplateResolver resolver)
        {
            ArgumentUtility.CheckForNull(resolver, nameof(resolver));
            m_resolvers.Add(resolver);
        }

        public IEnumerable<TaskStep> ResolveTasks(TaskTemplateStep step)
        {
            var resolver = m_resolvers.FirstOrDefault(x => x.CanResolve(step.Reference));
            if (resolver == null)
            {
                throw new NotSupportedException(PipelineStrings.TaskTemplateNotSupported(step.Reference.Name, step.Reference.Version));
            }

            return resolver.ResolveTasks(step);
        }

        private readonly IList<ITaskTemplateResolver> m_resolvers;
    }
}
