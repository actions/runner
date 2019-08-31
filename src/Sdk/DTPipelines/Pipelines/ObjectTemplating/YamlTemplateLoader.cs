using System;
using System.Collections.Generic;
using System.IO;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    /// <summary>
    /// Loads a YAML file, and returns the parsed TemplateToken
    /// </summary>
    internal sealed class YamlTemplateLoader
    {
        public YamlTemplateLoader(
            ParseOptions parseOptions,
            IFileProvider fileProvider)
        {
            m_parseOptions = new ParseOptions(parseOptions);
            m_fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        public TemplateToken LoadFile(
            TemplateContext context,
            Int32? rootFileId,
            String scope,
            String path,
            String templateType)
        {
            if (context.Errors.Count > 0)
            {
                throw new InvalidOperationException("Expected error count to be 0 when attempting to load a new file");
            }

            // Is entry file?
            var isEntryFile = m_referencedFiles.Count == 0;

            // Root the path
            path = m_fileProvider.ResolvePath(null, path);

            // Validate max files
            m_referencedFiles.Add(path);
            if (m_parseOptions.MaxFiles > 0 && m_referencedFiles.Count > m_parseOptions.MaxFiles)
            {
                throw new InvalidOperationException($"The maximum file count of {m_parseOptions.MaxFiles} has been exceeded");
            }

            // Get the file ID
            var fileId = context.GetFileId(path);

            // Check the cache
            if (!m_cache.TryGetValue(path, out String fileContent))
            {
                // Fetch the file
                context.CancellationToken.ThrowIfCancellationRequested();
                fileContent = m_fileProvider.GetFileContent(path);

                // Validate max file size
                if (fileContent.Length > m_parseOptions.MaxFileSize)
                {
                    throw new InvalidOperationException($"The maximum file size of {m_parseOptions.MaxFileSize} characters has been exceeded");
                }

                // Cache
                m_cache[path] = fileContent;
            }

            // Deserialize
            var token = default(TemplateToken);
            using (var stringReader = new StringReader(fileContent))
            {
                var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
                token = TemplateReader.Read(context, templateType, yamlObjectReader, fileId, out _);
            }

            // Trace
            if (!isEntryFile)
            {
                context.TraceWriter.Info(String.Empty);
            }
            context.TraceWriter.Info("# ");
            context.TraceWriter.Info("# {0}", path);
            context.TraceWriter.Info("# ");

            // Validate ref names
            if (context.Errors.Count == 0)
            {
                switch (templateType)
                {
                    case PipelineTemplateConstants.WorkflowRoot:
                        ValidateWorkflow(context, scope, token);
                        break;
                    case PipelineTemplateConstants.StepsTemplateRoot:
                        var stepsTemplate = token.AssertMapping("steps template");
                        foreach (var stepsTemplateProperty in stepsTemplate)
                        {
                            var stepsTemplatePropertyName = stepsTemplateProperty.Key.AssertString("steps template property name");
                            switch (stepsTemplatePropertyName.Value)
                            {
                                case PipelineTemplateConstants.Steps:
                                    ValidateSteps(context, scope, stepsTemplateProperty.Value);
                                    break;
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Unexpected template type '{templateType}' when loading yaml file");
                }
            }

            return token;
        }

        private void ValidateWorkflow(
            TemplateContext context,
            String scope,
            TemplateToken token)
        {
            var workflow = token.AssertMapping("workflow");
            foreach (var workflowProperty in workflow)
            {
                var workflowPropertyName = workflowProperty.Key.AssertString("workflow property name");
                switch (workflowPropertyName.Value)
                {
                    case PipelineTemplateConstants.Jobs:
                    case PipelineTemplateConstants.Workflow:
                        var jobs = workflowProperty.Value.AssertMapping("workflow property value");
                        foreach (var jobsProperty in jobs)
                        {
                            var job = jobsProperty.Value.AssertMapping("jobs property value");
                            foreach (var jobProperty in job)
                            {
                                var jobPropertyName = jobProperty.Key.AssertString("job property name");
                                switch (jobPropertyName.Value)
                                {
                                    case PipelineTemplateConstants.Steps:
                                        ValidateSteps(context, scope, jobProperty.Value);
                                        break;
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void ValidateSteps(
            TemplateContext context,
            String scope,
            TemplateToken token)
        {
            var nameBuilder = new ReferenceNameBuilder();
            var steps = token.AssertSequence("steps");
            var needsReferenceName = new List<MappingToken>();
            foreach (var stepsItem in steps)
            {
                var step = stepsItem.AssertMapping("steps item");
                var isTemplateReference = false;
                var hasReferenceName = false;
                foreach (var stepProperty in step)
                {
                    var stepPropertyKey = stepProperty.Key.AssertString("step property name");
                    switch (stepPropertyKey.Value)
                    {
                        // Validate reference names
                        case PipelineTemplateConstants.Id:
                            var referenceNameLiteral = stepProperty.Value.AssertString("step ID");
                            var referenceName = referenceNameLiteral.Value;
                            if (String.IsNullOrEmpty(referenceName))
                            {
                                continue;
                            }

                            if (!nameBuilder.TryAddKnownName(referenceName, out var error))
                            {
                                context.Error(referenceNameLiteral, error);
                            }

                            hasReferenceName = true;
                            break;

                        case PipelineTemplateConstants.Template:
                            isTemplateReference = true;
                            break;
                    }
                }

                // No reference name
                if (isTemplateReference && !hasReferenceName)
                {
                    needsReferenceName.Add(step);
                }

                // Stamp the scope
                if (!String.IsNullOrEmpty(scope))
                {
                    var scopePropertyName = new StringToken(null, null, null, PipelineTemplateConstants.Scope);
                    var scopePropertyValue = new StringToken(null, null, null, scope);
                    step.Add(scopePropertyName, scopePropertyValue);
                    context.Memory.AddBytes(scopePropertyName);
                    context.Memory.AddBytes(scopePropertyValue);
                }
            }

            // Generate reference names
            if (needsReferenceName.Count > 0 && context.Errors.Count == 0)
            {
                foreach (var step in needsReferenceName)
                {
                    // Get the template path
                    var templatePath = default(String);
                    foreach (var stepProperty in step)
                    {
                        var stepPropertyKey = stepProperty.Key.AssertString("step property name");
                        switch (stepPropertyKey.Value)
                        {
                            case PipelineTemplateConstants.Template:
                                var templateStringToken = stepProperty.Value.AssertString("step template path");
                                templatePath = templateStringToken.Value;
                                break;
                        }
                    }

                    // Generate reference name
                    if (!String.IsNullOrEmpty(templatePath))
                    {
                        nameBuilder.AppendSegment(templatePath);
                        var generatedIdPropertyName = new StringToken(null, null, null, PipelineTemplateConstants.GeneratedId);
                        var generatedIdPropertyValue = new StringToken(null, null, null, nameBuilder.Build());
                        step.Add(generatedIdPropertyName, generatedIdPropertyValue);
                        context.Memory.AddBytes(generatedIdPropertyName);
                        context.Memory.AddBytes(generatedIdPropertyValue);
                    }
                }
            }
        }

        /// <summary>
        /// Cache of file content
        /// </summary>
        private readonly Dictionary<String, String> m_cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly IFileProvider m_fileProvider;

        private readonly ParseOptions m_parseOptions;

        /// <summary>
        /// Tracks unique file references
        /// </summary>
        private readonly HashSet<String> m_referencedFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    }
}
