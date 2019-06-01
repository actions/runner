using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PipelineUtilities
    {
        public static Guid GetInstanceId(
            String identifier,
            Boolean preserveCase = false)
        {
            if (preserveCase)
            {
                return TimelineRecordIdGenerator.GetId(identifier);
            }
            else
            {
                return TimelineRecordIdGenerator.GetId(identifier?.ToLowerInvariant());
            }
        }

        public static String GetInstanceName(params String[] segments)
        {
            return String.Join(".", segments.Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim('.')));
        }

        public static String GetName(String identifier)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(identifier, nameof(identifier));

            var separatorIndex = identifier.LastIndexOf('.');
            return separatorIndex >= 0 ? identifier.Substring(separatorIndex + 1) : identifier;
        }

        public static Guid GetStageInstanceId(
            StageInstance stage,
            Boolean preserveCase = false)
        {
            return GetStageInstanceId(stage.Name, stage.Attempt, preserveCase);
        }

        public static String GetStageIdentifier(StageInstance stage)
        {
            return GetStageIdentifier(stage.Name);
        }

        public static String GetStageIdentifier(String stageName)
        {
            return GetStageInstanceName(stageName, 1);
        }

        public static String GetStageInstanceName(StageInstance stage)
        {
            return GetStageInstanceName(stage.Name, stage.Attempt);
        }

        public static Guid GetStageInstanceId(
            String stageName,
            Int32 stageAttempt,
            Boolean preserveCase = false)
        {
            return GetInstanceId(GetStageInstanceName(stageName, stageAttempt, true), preserveCase);
        }

        public static String GetStageInstanceName(
            String stageName,
            Int32 stageAttempt)
        {
            return GetStageInstanceName(stageName, stageAttempt, true);
        }

        public static String GetStageInstanceName(
            String stageName,
            Int32 stageAttempt,
            Boolean includeDefault)
        {
            if (!String.IsNullOrEmpty(stageName) &&
                (includeDefault || !stageName.Equals(PipelineConstants.DefaultJobName, StringComparison.OrdinalIgnoreCase)))
            {
                var instanceName = stageName;
                if (stageAttempt > 1)
                {
                    instanceName = $"{stageName}.{stageAttempt}";
                }

                return instanceName;
            }

            return String.Empty;
        }

        public static String GetPhaseIdentifier(
            StageInstance stage,
            PhaseInstance phase)
        {
            return GetPhaseIdentifier(stage?.Name, phase.Name);
        }

        public static String GetPhaseIdentifier(
            String stageName,
            String phaseName)
        {
            return GetPhaseInstanceName(stageName, phaseName, 1);
        }

        public static Guid GetPhaseInstanceId(
            StageInstance stage,
            PhaseInstance phase,
            Boolean preserveCase = false)
        {
            return GetPhaseInstanceId(stage?.Name, phase.Name, phase.Attempt, preserveCase);
        }

        public static Guid GetPhaseInstanceId(
            String stageName,
            String phaseName,
            Int32 phaseAttempt,
            Boolean preserveCase = false)
        {
            return GetInstanceId(GetPhaseInstanceName(stageName, phaseName, phaseAttempt), preserveCase);
        }

        public static String GetPhaseInstanceName(
            StageInstance stage,
            PhaseInstance phase)
        {
            var sb = new StringBuilder(GetStageInstanceName(stage?.Name, 1, false));
            if (sb.Length > 0)
            {
                sb.Append(".");
            }

            sb.Append($"{phase.Name}");
            if (phase.Attempt > 1)
            {
                sb.Append($".{phase.Attempt}");
            }

            return sb.ToString();
        }

        public static String GetPhaseInstanceName(
            String stageName,
            String phaseName,
            Int32 phaseAttempt)
        {
            var sb = new StringBuilder(GetStageInstanceName(stageName, 1, false));
            if (sb.Length > 0)
            {
                sb.Append(".");
            }

            sb.Append($"{phaseName}");
            if (phaseAttempt > 1)
            {
                sb.Append($".{phaseAttempt}");
            }

            return sb.ToString();
        }

        public static String GetJobIdentifier(
            StageInstance stage,
            PhaseInstance phase,
            JobInstance job)
        {
            return GetJobIdentifier(stage?.Name, phase.Name, job.Name);
        }

        public static String GetJobIdentifier(
            String stageName,
            String phaseName,
            String jobName)
        {
            return GetJobInstanceName(stageName, phaseName, jobName, 1);
        }

        public static Guid GetJobInstanceId(
            StageInstance stage,
            PhaseInstance phase,
            JobInstance job,
            Boolean preserveCase = false)
        {
            return GetJobInstanceId(stage?.Name, phase.Name, job.Name, job.Attempt, preserveCase);
        }

        public static Guid GetJobInstanceId(
            String stageName,
            String phaseName,
            String jobName,
            Int32 jobAttempt,
            Boolean preserveCase = false)
        {
            return GetInstanceId(GetJobInstanceName(stageName, phaseName, jobName, jobAttempt), preserveCase);
        }

        public static String GetJobInstanceName(
            StageInstance stage,
            PhaseInstance phase,
            JobInstance job)
        {
            return GetJobInstanceName(stage?.Name, phase.Name, job.Name, job.Attempt);
        }

        public static String GetJobInstanceName(
            String jobIdentifier,
            Int32 jobAttempt)
        {
            var sb = new StringBuilder(jobIdentifier);
            if (jobAttempt > 1)
            {
                sb.Append($".{jobAttempt}");
            }

            return sb.ToString();
        }

        public static String GetJobInstanceName(TimelineRecord job)
        {
            if (job.Attempt <= 1)
            {
                return job.Identifier;
            }
            else
            {
                return $"{job.Identifier}.{job.Attempt}";
            }
        }

        public static String GetJobInstanceName(
            String stageName,
            String phaseName,
            String jobName,
            Int32 jobAttempt)
        {
            var sb = new StringBuilder(GetPhaseInstanceName(stageName, phaseName, 1));
            sb.Append($".{jobName}");
            if (jobAttempt > 1)
            {
                sb.Append($".{jobAttempt}");
            }

            return sb.ToString();
        }

        public static Guid GetTaskInstanceId(
            String stageName,
            String phaseName,
            String jobName,
            Int32 jobAttempt,
            String taskName,
            Boolean preserveCase = false)
        {
            return GetInstanceId(GetTaskInstanceName(stageName, phaseName, jobName, jobAttempt, taskName), preserveCase);
        }

        public static String GetTaskInstanceName(
            String stageName,
            String phaseName,
            String jobName,
            Int32 jobAttempt,
            String taskName)
        {
            return $"{GetJobInstanceName(stageName, phaseName, jobName, jobAttempt)}.{taskName}";
        }

        public static String GetTaskInstanceName(
            TimelineRecord jobRecord,
            TimelineRecord taskRecord)
        {
            return $"{GetJobInstanceName(jobRecord)}.{taskRecord.RefName}";
        }

        public static TaskResult MergeResult(
            TaskResult result,
            TaskResult childResult)
        {
            // If the final status is already failed then we can't get any worse
            if (result == TaskResult.Canceled || result == TaskResult.Failed)
            {
                return result;
            }

            switch (childResult)
            {
                case TaskResult.Canceled:
                    result = TaskResult.Canceled;
                    break;

                case TaskResult.Failed:
                case TaskResult.Abandoned:
                    result = TaskResult.Failed;
                    break;

                case TaskResult.SucceededWithIssues:
                    if (result == TaskResult.Succeeded)
                    {
                        result = TaskResult.SucceededWithIssues;
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// returns the node path from pipeline root to instance node
        /// </summary>
        public static IList<String> GetPathComponents(String instanceName)
        {
            var result = new List<String>();
            if (!String.IsNullOrEmpty(instanceName))
            {
                var tokens = instanceName.Split('.');
                var i = 0;
                if (Guid.TryParse(tokens[i], out var _))
                {
                    // first parameter might be a guid
                    i = 1;
                }

                // ignore attempt numbers -- these are not meaningful as path components
                for (var end = tokens.Length; i < end; ++i)
                {
                    var t = tokens[i];

                    // node names may only contain numbers, letters, and '_'
                    // node names must begin with at letter. 
                    result.AddIf(!Int32.TryParse(t, out var _), t);
                }
            }

            return result;
        }

        /// <summary>
        /// A legal node name starts with a letter or '_', and is entirely 
        /// comprised of alphanumeric characters or the ['_', '-'] characters.
        /// </summary>
        public static Boolean IsLegalNodeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (name.Length > PipelineConstants.MaxNodeNameLength)
            {
                return false;
            }

            if (!char.IsLetter(name[0]))
            {
                return false;
            }

            foreach (var c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
