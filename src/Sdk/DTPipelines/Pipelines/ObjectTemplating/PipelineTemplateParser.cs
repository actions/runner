using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.ObjectTemplating.Schema;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    using GitHub.DistributedTask.ObjectTemplating;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineTemplateParser
    {
        static PipelineTemplateParser()
        {
            var schemaFactory = new PipelineTemplateSchemaFactory();
            s_schema = schemaFactory.CreateSchema();
        }

        public PipelineTemplateParser(
            ITraceWriter trace,
            ParseOptions options)
        {
            m_trace = trace ?? throw new ArgumentNullException(nameof(trace));
            m_parseOptions = new ParseOptions(options ?? throw new ArgumentNullException(nameof(options)));
        }

        /// <summary>
        /// Loads the YAML pipeline template
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown when the entry YAML file does not exist</exception>
        public PipelineTemplate LoadPipeline(
            IFileProvider fileProvider,
            RepositoryResource self,
            String path,
            CancellationToken cancellationToken)
        {
            fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            self = self ?? throw new ArgumentNullException(nameof(self));
            var parseResult = LoadPipelineInternal(fileProvider, path, cancellationToken);
            return PipelineTemplateConverter.ConvertToPipeline(parseResult.Context, self, parseResult.Value);
        }

        internal ParseResult LoadPipelineInternal(
            IFileProvider fileProvider,
            String path,
            CancellationToken cancellationToken)
        {
            // Setup the context
            var templateLoader = new YamlTemplateLoader(new ParseOptions(m_parseOptions), fileProvider);
            var context = new TemplateContext
            {
                CancellationToken = cancellationToken,
                Errors = new TemplateValidationErrors(m_parseOptions.MaxErrors, m_parseOptions.MaxErrorMessageLength),
                Memory = new TemplateMemory(
                    maxDepth: m_parseOptions.MaxDepth,
                    maxEvents: m_parseOptions.MaxParseEvents,
                    maxBytes: m_parseOptions.MaxResultSize),
                Schema = s_schema,
                TraceWriter = m_trace,
            };

            // Load the entry file
            var token = default(TemplateToken);
            try
            {
                token = templateLoader.LoadFile(context, null, null, path, PipelineTemplateConstants.WorkflowRoot);
            }
            catch (Exception ex)
            {
                context.Errors.Add(ex);
            }

            var result = new ParseResult
            {
                Context = context,
                Value = token,
            };

            if (token != null && context.Errors.Count == 0)
            {
                var templateReferenceCount = ResolveWorkflowTemplateReferences(context, templateLoader, token);
                if (templateReferenceCount > 0 && context.Errors.Count == 0)
                {
                    context.TraceWriter.Info(String.Empty);
                    context.TraceWriter.Info("# ");
                    context.TraceWriter.Info("# Template resolution complete. Final runtime YAML document:");
                    context.TraceWriter.Info("# ");
                    context.TraceWriter.Info("{0}", result.ToYaml());
                }
            }

            return result;
        }

        private Int32 ResolveWorkflowTemplateReferences(
            TemplateContext context,
            YamlTemplateLoader templateLoader,
            TemplateToken token)
        {
            var resolvedCount = 0;
            var workflow = token.AssertMapping("workflow");
            foreach (var workflowProperty in workflow)
            {
                var workflowPropertyName = workflowProperty.Key.AssertString("workflow property");
                switch (workflowPropertyName.Value)
                {
                    case PipelineTemplateConstants.Jobs:
                        resolvedCount += ResolveJobsTemplateReferences(context, templateLoader, workflowProperty.Value);
                        break;

                    case PipelineTemplateConstants.Workflow:
                        resolvedCount += ResolveJobsTemplateReferences(context, templateLoader, workflowProperty.Value);
                        break;
                }
            }

            return resolvedCount;
        }

        private Int32 ResolveJobsTemplateReferences(
            TemplateContext context,
            YamlTemplateLoader templateLoader,
            TemplateToken token)
        {
            var resolvedCount = 0;
            var jobs = token.AssertMapping("jobs");
            foreach (var jobsProperty in jobs)
            {
                var job = jobsProperty.Value.AssertMapping("jobs property value");
                var scopes = new SequenceToken(null, null, null);
                foreach (var jobProperty in job)
                {
                    var jobPropertyName = jobProperty.Key.AssertString("job property name");
                    switch (jobPropertyName.Value)
                    {
                        case PipelineTemplateConstants.Steps:
                            resolvedCount += ResolveStepsTemplateReferences(context, templateLoader, jobProperty.Value, scopes);
                            break;
                    }
                }

                if (scopes.Count > 0)
                {
                    var scopesPropertyName = new StringToken(null, null, null, PipelineTemplateConstants.Scopes);
                    job.Add(scopesPropertyName, scopes);
                    context.Memory.AddBytes(scopesPropertyName);
                    context.Memory.AddBytes(scopes); // Do not traverse, nested objects already accounted for
                }
            }

            return resolvedCount;
        }

        private Int32 ResolveStepsTemplateReferences(
            TemplateContext context,
            YamlTemplateLoader templateLoader,
            TemplateToken token,
            SequenceToken scopes)
        {
            var resolvedCount = 0;
            var steps = token.AssertSequence("steps");
            var stepIndex = 0;
            while (stepIndex < steps.Count && context.Errors.Count == 0)
            {
                var step = steps[stepIndex].AssertMapping("step");
                if (!TemplateReference.TryCreate(step, out var reference))
                {
                    stepIndex++;
                    continue;
                }

                resolvedCount++;
                var template = templateLoader.LoadFile(
                    context,
                    reference.TemplatePath.FileId,
                    reference.TemplateScope,
                    reference.TemplatePath.Value,
                    PipelineTemplateConstants.StepsTemplateRoot);

                if (context.Errors.Count != 0)
                {
                    break;
                }

                var scope = reference.CreateScope(context, template);

                if (context.Errors.Count != 0)
                {
                    break;
                }

                // Remove the template reference and memory overhead
                steps.RemoveAt(stepIndex);
                context.Memory.SubtractBytes(step, true); // Traverse

                // Remove the template memory overhead
                context.Memory.SubtractBytes(template, true); // Traverse

                var templateSteps = GetSteps(template);
                if (templateSteps?.Count > 0)
                {
                    // Add the steps from the template
                    steps.InsertRange(stepIndex, templateSteps);
                    context.Memory.AddBytes(templateSteps, true); // Traverse
                    context.Memory.SubtractBytes(templateSteps, false);

                    // Add the scope
                    scopes.Add(scope);
                    context.Memory.AddBytes(scope, true); // Traverse
                }
            }

            return resolvedCount;
        }

        private SequenceToken GetSteps(TemplateToken template)
        {
            var mapping = template.AssertMapping("steps template");
            foreach (var property in mapping)
            {
                var propertyName = property.Key.AssertString("steps template property name");
                switch (propertyName.Value)
                {
                    case PipelineTemplateConstants.Steps:
                        return property.Value.AssertSequence("steps template steps property value");
                }
            }

            return null;
        }

        private static TemplateSchema s_schema;
        private readonly ParseOptions m_parseOptions;
        private readonly ITraceWriter m_trace;
    }
}
