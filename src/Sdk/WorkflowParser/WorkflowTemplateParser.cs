#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Threading;
using GitHub.Actions.WorkflowParser.Conversion;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    using GitHub.Actions.WorkflowParser.ObjectTemplating;

    /// <summary>
    /// Parses a workflow YAML file.
    /// </summary>
    public sealed class WorkflowTemplateParser
    {
        public WorkflowTemplateParser(
            IServerTraceWriter serverTrace,
            ITraceWriter trace,
            ParseOptions options,
            WorkflowFeatures features)
        {
            m_serverTrace = serverTrace ?? new EmptyServerTraceWriter();
            m_trace = trace ?? new EmptyTraceWriter();
            m_parseOptions = new ParseOptions(options ?? throw new ArgumentNullException(nameof(options)));
            m_features = features ?? WorkflowFeatures.GetDefaults();
        }

        /// <summary>
        /// Loads the YAML workflow template
        /// </summary>
        public WorkflowTemplate LoadWorkflow(
            IFileProvider fileProvider,
            String path,
            String permissionPolicy,
            IDictionary<string, ReferencedWorkflow> referencedWorkflows,
            CancellationToken cancellationToken)
        {
            (var result, _) = LoadWorkflowInternal(fileProvider, path, permissionPolicy, referencedWorkflows, cancellationToken);
            return result;
        }

        /// <summary>
        /// Loads the YAML workflow template and the estimated number of bytes consumed in memory (for x-lang unit tests)
        /// </summary>
        /// <returns>The workflow template, and the estimated number of bytes consumed in memory</returns>
        internal (WorkflowTemplate, int) LoadWorkflowInternal(
            IFileProvider fileProvider,
            String path,
            String permissionPolicy,
            IDictionary<string, ReferencedWorkflow> referencedWorkflows,
            CancellationToken cancellationToken)

        {
            fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            TemplateContext context;
            YamlTemplateLoader loader;
            TemplateToken tokens;

            // Parse template tokens
            (context, loader, tokens) = ParseTemplate(fileProvider, path, cancellationToken);

            var usage = new WorkflowUsage(m_serverTrace);
            try
            {
                // Gather telemetry
                usage.Gather(context, tokens);

                // Convert to workflow types
                var workflowTemplate = WorkflowTemplateConverter.ConvertToWorkflow(context, tokens);

                // Set telemetry
                workflowTemplate.Telemetry = context.Telemetry;

                // Load reusable workflows
                ReusableWorkflowsLoader.Load(m_serverTrace, m_trace, m_parseOptions, usage, context, workflowTemplate, loader, permissionPolicy, referencedWorkflows);

                // Error state? Throw away the model
                if (workflowTemplate.Errors.Count > 0)
                {
                    var errorTemplate = new WorkflowTemplate();
                    errorTemplate.Errors.AddRange(workflowTemplate.Errors);
                    errorTemplate.FileTable.AddRange(workflowTemplate.FileTable);
                    errorTemplate.Telemetry = context.Telemetry;
                    workflowTemplate = errorTemplate;
                }

                return (workflowTemplate, context.Memory.CurrentBytes);
            }
            finally
            {
                usage.Trace();
            }
        }

        /// <summary>
        /// Parses a workflow template file.
        /// <summary>
        private (TemplateContext, YamlTemplateLoader, TemplateToken) ParseTemplate(
            IFileProvider fileProvider,
            String path,
            CancellationToken cancellationToken)
        {
            // Setup the template context
            var context = new TemplateContext
            {
                CancellationToken = cancellationToken,
                Errors = new TemplateValidationErrors(m_parseOptions.MaxErrors, m_parseOptions.MaxErrorMessageLength),
                Memory = new TemplateMemory(
                    maxDepth: m_parseOptions.MaxDepth,
                    maxEvents: m_parseOptions.MaxParseEvents,
                    maxBytes: m_parseOptions.MaxResultSize),
                Schema = WorkflowSchemaFactory.GetSchema(m_features),
                TraceWriter = m_trace,
            };
            context.SetFeatures(m_features);
            context.SetJobCountValidator(new JobCountValidator(context, m_parseOptions.MaxJobLimit));

            // Setup the template loader
            var loader = new YamlTemplateLoader(new ParseOptions(m_parseOptions), fileProvider);

            // Parse the template tokens
            var tokens = loader.ParseWorkflow(context, path);

            return (context, loader, tokens);
        }

        private readonly WorkflowFeatures m_features;
        private readonly ParseOptions m_parseOptions;
        private readonly IServerTraceWriter m_serverTrace;
        private readonly ITraceWriter m_trace;
    }
}
