using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Artifacts
{
    /// <summary>
    /// Provides a mechanism to resolve the artifacts
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IArtifactResolver
    {
        /// <summary>
        /// Given a resource, it gets the corresponding task id from its extension
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        Guid GetArtifactDownloadTaskId(Resource resource);

        /// <summary>
        /// Given a resource and step, it maps the resource properties to task inputs
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="taskStep"></param>
        void PopulateMappedTaskInputs(Resource resource, TaskStep taskStep);

        /// <summary>
        /// Given an artifact step, it resolves the artifact and returns a download artifact task
        /// </summary>
        /// <param name="pipelineContext"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        Boolean ResolveStep(IPipelineContext pipelineContext, JobStep step, out IList<TaskStep> resolvedSteps);

        /// <summary>
        /// Given resource store and task step it translate the taskStep into actual task reference with mapped inputs
        /// </summary>
        /// <param name="resourceStore"></param>
        /// <param name="taskStep"></param>
        /// <returns></returns>
        Boolean ResolveStep(IResourceStore resourceStore, TaskStep taskStep, out String errorMessage);

        /// <summary>
        /// Validate the given resource in the YAML file. Also resolve version for the resource if not resolved already
        /// </summary>
        /// <param name="resources"></param>
        Boolean ValidateDeclaredResource(Resource resource, out PipelineValidationError error);
    }
}