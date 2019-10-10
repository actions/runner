using System;
using System.Collections.Generic;

using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.Artifacts;
namespace GitHub.DistributedTask.Orchestration.Server.Artifacts
{
    public static class DownloadStepExtensions
    {
        public static Boolean IsDownloadBuildStepExists(this IReadOnlyList<JobStep> steps)
        {
            foreach (var step in steps)
            {
                if (step is TaskStep taskStep)
                {
                    if (taskStep.IsDownloadBuildTask())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static Boolean IsDownloadBuildTask(this Step step)
        {
            if (step is TaskStep taskStep &&
                taskStep.Reference != null &&
                taskStep.Reference.Name.Equals(YamlArtifactConstants.DownloadBuild, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static Boolean IsDownloadStepDisabled(this Step step)
        {
            // either download task or downloadBuild task has none keyword return true.
            if (step is TaskStep taskStep &&
                taskStep.Inputs.TryGetValue(PipelineArtifactConstants.DownloadTaskInputs.Alias, out String alias) &&
                String.Equals(alias, YamlArtifactConstants.None, StringComparison.OrdinalIgnoreCase) &&
                (step.IsDownloadBuildTask() || step.IsDownloadTask()))
            {
                return true;
            }

            return false;
        }

        public static Boolean IsDownloadTask(this Step step)
        {
            if (step is TaskStep taskStep &&
                taskStep.Reference != null &&
                taskStep.Reference.Id.Equals(PipelineArtifactConstants.DownloadTask.Id) &&
                taskStep.Reference.Version == PipelineArtifactConstants.DownloadTask.Version)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Boolean IsDownloadCurrentPipelineArtifactStep(this Step step)
        {
            if (step is TaskStep taskStep && 
                taskStep.IsDownloadTask() &&
                taskStep.Inputs.TryGetValue(PipelineArtifactConstants.DownloadTaskInputs.Alias, out String alias) &&
                String.Equals(alias, YamlArtifactConstants.Current, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static Boolean IsDownloadPipelineArtifactStepDisabled(this TaskStep step)
        {
            if (step.IsDownloadTask() &&
                step.Inputs.TryGetValue(PipelineArtifactConstants.DownloadTaskInputs.Alias, out String alias) &&
                String.Equals(alias, YamlArtifactConstants.None, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static Boolean IsDownloadExternalPipelineArtifactStep(this TaskStep step)
        {
            if (step.IsDownloadTask() && 
                step.Inputs != null &&
                step.Inputs.TryGetValue(PipelineArtifactConstants.DownloadTaskInputs.Alias, out String alias) &&
                !String.IsNullOrEmpty(alias) &&
                !alias.Equals(YamlArtifactConstants.Current, StringComparison.OrdinalIgnoreCase) &&
                !alias.Equals(YamlArtifactConstants.None, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static String GetAliasFromTaskStep(this TaskStep step)
        {
            return step.Inputs.TryGetValue(PipelineArtifactConstants.DownloadTaskInputs.Alias, out String alias)
                       ? alias
                       : String.Empty;
        }

        public static Boolean IsDownloadPipelineArtifactStepExists(this IReadOnlyList<JobStep> steps)
        {
            foreach (var step in steps)
            {
                if (step is TaskStep taskStep)
                {
                    if (taskStep.IsDownloadTask())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Merge(
            this IDictionary<String, String> first, 
            IDictionary<String, String> second)
        {
            foreach (var key in second?.Keys ?? new List<String>())
            {
                first[key] = second[key];
            }
        }

        public static void Merge(
            this IDictionary<String, String> first, 
            IReadOnlyDictionary<String, String> second)
        {
            foreach (var key in second?.Keys ?? new List<String>())
            {
                first[key] = second[key];
            }
        }
    }
}
