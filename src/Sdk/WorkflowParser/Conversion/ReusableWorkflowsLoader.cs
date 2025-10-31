#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Actions.Expressions;
using GitHub.Actions.Expressions.Sdk;
using GitHub.Actions.WorkflowParser.Conversion;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    using GitHub.Actions.WorkflowParser.ObjectTemplating;

    /// <summary>
    /// Loads reusable workflows
    /// </summary>
    internal sealed class ReusableWorkflowsLoader
    {
        private ReusableWorkflowsLoader(
            IServerTraceWriter serverTrace,
            ITraceWriter trace,
            ParseOptions options,
            WorkflowUsage usage,
            TemplateContext context,
            WorkflowTemplate workflowTemplate,
            YamlTemplateLoader loader,
            String permissionPolicy,
            IDictionary<string, ReferencedWorkflow> referencedWorkflows)
        {
            m_serverTrace = serverTrace ?? new EmptyServerTraceWriter();
            m_trace = trace ?? new EmptyTraceWriter();
            m_parseOptions = new ParseOptions(options ?? throw new ArgumentNullException(nameof(options)));
            m_usage = usage ?? throw new ArgumentNullException(nameof(usage));
            m_context = context ?? throw new ArgumentNullException(nameof(context));
            m_workflowTemplate = workflowTemplate ?? throw new ArgumentNullException(nameof(workflowTemplate));
            m_loader = loader ?? throw new ArgumentNullException(nameof(loader));
            m_permissionPolicy = permissionPolicy ?? throw new ArgumentNullException(nameof(permissionPolicy));
            m_referencedWorkflows = referencedWorkflows ?? throw new ArgumentNullException(nameof(referencedWorkflows));
        }

        /// <summary>
        /// Loads reusable workflows if not in an error state.
        ///
        /// Any new errors are recorded to both <c ref="TemplateContext.Errors" /> and <c ref="WorkflowTemplate.Errors" />.
        /// </summary>
        public static void Load(
            IServerTraceWriter serverTrace,
            ITraceWriter trace,
            ParseOptions options,
            WorkflowUsage usage,
            TemplateContext context,
            WorkflowTemplate workflowTemplate,
            YamlTemplateLoader loader,
            String permissionPolicy,
            IDictionary<string, ReferencedWorkflow> referencedWorkflows)
        {
            new ReusableWorkflowsLoader(serverTrace, trace, options, usage, context, workflowTemplate, loader, permissionPolicy, referencedWorkflows)
                .Load();
        }

        /// <summary>
        /// Refer overload
        /// </summary>
        private void Load()
        {
            // Skip reusable workflows?
            if (m_parseOptions.SkipReusableWorkflows)
            {
                return;
            }

            // Check errors
            if (m_context.Errors.Count > 0)
            {
                return;
            }

            // Note, the "finally" block appends context.Errors to workflowTemplate
            var hasReusableWorkflowJob = false;
            try
            {
                foreach (var job in m_workflowTemplate.Jobs)
                {
                    // Load reusable workflow
                    if (job is ReusableWorkflowJob workflowJob)
                    {
                        hasReusableWorkflowJob = true;
                        LoadRecursive(workflowJob);

                        // Check errors
                        if (m_context.Errors.Count > 0)
                        {
                            return;
                        }
                    }
                }
            }
            catch (ReferencedWorkflowNotFoundException)
            {
                // Long term, catch TemplateUserException and let others bubble
                throw;
            }
            catch (Exception ex)
            {
                m_context.Errors.Add(ex);
            }
            finally
            {
                // Append context.Errors to workflowTemplate
                if (m_context.Errors.Count > 0)
                {
                    foreach (var error in m_context.Errors)
                    {
                        m_workflowTemplate.Errors.Add(new WorkflowValidationError(error.Code, error.Message));
                    }
                }

                // Update WorkflowTemplate.FileTable with referenced workflows
                if (hasReusableWorkflowJob)
                {
                    m_workflowTemplate.FileTable.Clear();
                    m_workflowTemplate.FileTable.AddRange(m_context.GetFileTable());
                }
            }
        }

        /// <summary>
        /// This loads referenced workflow by parsing the workflow file and converting to workflow template WorkflowJob.
        /// </summary>
        private void LoadRecursive(
            ReusableWorkflowJob workflowJob,
            int depth = 1)
        {
            // Check depth
            if (depth > m_parseOptions.MaxNestedReusableWorkflowsDepth)
            {
                throw new Exception($"Nested reusable workflow depth exceeded {m_parseOptions.MaxNestedReusableWorkflowsDepth}.");
            }

            TemplateToken tokens;

            // Load the reusable workflow
            try
            {
                // Fully qualify workflow ref
                workflowJob.Ref = FullyQualifyWorkflowRef(m_context, workflowJob.Ref, m_referencedWorkflows);
                var isTrusted = IsReferencedWorkflowTrusted(workflowJob.Ref.Value);

                // Parse template tokens
                tokens = m_loader.ParseWorkflow(m_context, workflowJob.Ref.Value);

                // Gather telemetry
                m_usage.Gather(m_context, tokens);

                // Check errors
                if (m_context.Errors.Count > 0)
                {
                    // Short-circuit
                    return;
                }

                // Convert to workflow types
                WorkflowTemplateConverter.ConvertToReferencedWorkflow(m_context, tokens, workflowJob, m_permissionPolicy, isTrusted);

                // Check errors
                if (m_context.Errors.Count > 0)
                {
                    // Short-circuit
                    return;
                }
            }
            finally
            {
                // Prefix errors with caller file/line/col
                PrefixErrorsWithCallerInfo(workflowJob);
            }

            // Load nested reusable workflows
            foreach (var nestedJob in workflowJob.Jobs)
            {
                if (nestedJob is ReusableWorkflowJob nestedWorkflowJob)
                {
                    // Recurse
                    LoadRecursive(nestedWorkflowJob, depth + 1);

                    // Check errors
                    if (m_context.Errors.Count > 0)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// For the given token and referencedWorkflows, resolve the workflow reference (i.e. token value)
        /// This ensures that the workflow reference is the fully qualified form (nwo+path+version) even when calling local workflows without nwo or version
        /// </summary>
        internal static StringToken FullyQualifyWorkflowRef(
            TemplateContext context,
            StringToken workflowJobRef,
            IDictionary<string, ReferencedWorkflow> referencedWorkflows)
        {
            if (!workflowJobRef.Value.StartsWith(WorkflowTemplateConstants.LocalPrefix))
            {
                return workflowJobRef;
            }

            var callerPath = context.GetFileName(workflowJobRef.FileId.Value);
            if (!referencedWorkflows.TryGetValue(callerPath, out ReferencedWorkflow callerWorkflow) || callerWorkflow == null)
            {
                throw new ReferencedWorkflowNotFoundException($"Cannot find the caller workflow from the referenced workflows: '{callerPath}'");
            }

            var filePath = workflowJobRef.Value.Substring(WorkflowTemplateConstants.LocalPrefix.Length);
            var path = $"{callerWorkflow.Repository}/{filePath}@{callerWorkflow.ResolvedSha}";

            return new StringToken(workflowJobRef.FileId, workflowJobRef.Line, workflowJobRef.Column, path);
        }

        /// <summary>
        /// Prefixes all error messages with the caller file/line/column.
        /// </summary>
        private void PrefixErrorsWithCallerInfo(ReusableWorkflowJob workflowJob)
        {
            if (m_context.Errors.Count == 0)
            {
                return;
            }

            var callerFile = m_context.GetFileName(workflowJob.Ref.FileId.Value);
            for (int i = 0; i < m_context.Errors.Count; i++)
            {
                var errorMessage = m_context.Errors.GetMessage(i);
                if (String.IsNullOrEmpty(errorMessage) || !errorMessage.StartsWith(callerFile))
                {
                    // when there is no caller file in the error message, we add it for annotation
                    m_context.Errors.PrefixMessage(
                        i,
                        TemplateStrings.CalledWorkflowNotValidWithErrors(
                            callerFile,
                            TemplateStrings.LineColumn(workflowJob.Ref.Line, workflowJob.Ref.Column)));
                }
            }
        }

        /// <summary>
        /// Checks if the given workflowJobRefValue is trusted
        /// </summary>
        private bool IsReferencedWorkflowTrusted(String workflowJobRefValue)
        {
            if (m_referencedWorkflows.TryGetValue(workflowJobRefValue, out ReferencedWorkflow referencedWorkflow) &&
                referencedWorkflow != null)
            {
                return referencedWorkflow.IsTrusted();
            }

            return false;
        }

        private readonly TemplateContext m_context;
        private readonly YamlTemplateLoader m_loader;
        private readonly ParseOptions m_parseOptions;
        private readonly string m_permissionPolicy;
        private readonly IDictionary<string, ReferencedWorkflow> m_referencedWorkflows;
        private readonly IServerTraceWriter m_serverTrace;
        private readonly ITraceWriter m_trace;
        private readonly WorkflowUsage m_usage;
        private readonly WorkflowTemplate m_workflowTemplate;
    }
}
