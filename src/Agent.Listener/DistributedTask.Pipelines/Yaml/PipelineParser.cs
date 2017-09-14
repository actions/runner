using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.TypeConverters;
using YamlDotNet.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineParser
    {
        public PipelineParser(ITraceWriter trace, IFileProvider fileProvider, ParseOptions options)
        {
            if (trace == null)
            {
                throw new ArgumentNullException(nameof(trace));
            }

            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            m_trace = trace;
            m_fileProvider = fileProvider;
            m_options = new ParseOptions(options);
        }

        /// <summary>
        /// This is for internal unit testing only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public String DeserializeAndSerialize(String defaultRoot, String path, IDictionary<String, Object> mustacheContext, CancellationToken cancellationToken)
        {
            Int32 fileCount = 0;

            // Load the target file.
            path = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: path);
            PipelineFile<Process> processFile = LoadFile<Process, ProcessConverter>(path, mustacheContext, cancellationToken, ref fileCount);
            Process process = processFile.Object;
            ResolveTemplates(process, defaultRoot: processFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);

            // Serialize
            SerializerBuilder serializerBuilder = new SerializerBuilder();
            serializerBuilder.DisableAliases();
            serializerBuilder.WithTypeConverter(new ProcessConverter());
            Serializer serializer = serializerBuilder.Build();
            return serializer.Serialize(process);
        }

        // TODO: CHANGE THIS TO PUBLIC WHEN SWITCH RETURN TYPES
        internal Process LoadInternal(String defaultRoot, String path, IDictionary<String, Object> mustacheContext, CancellationToken cancellationToken)
        {
            Int32 fileCount = 0;

            // Load the target file.
            path = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: path);
            PipelineFile<Process> processFile = LoadFile<Process, ProcessConverter>(path, mustacheContext, cancellationToken, ref fileCount);
            Process process = processFile.Object;
            ResolveTemplates(process, defaultRoot: processFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);

            // Create implied levels for the process.
            if (process.Steps != null)
            {
                var newPhase = new Phase
                {
                    Name = process.Name,
                    Condition = process.Condition,
                    ContinueOnError = process.ContinueOnError,
                    DependsOn = process.DependsOn,
                    EnableAccessToken = process.EnableAccessToken,
                    Steps = process.Steps,
                    Target = process.Target,
                    Variables = process.Variables,
                };
                process.Phases = new List<IPhase>();
                process.Phases.Add(newPhase);
                process.Condition = null;
                process.ContinueOnError = null;
                process.DependsOn = null;
                process.EnableAccessToken = null;
                process.Steps = null;
                process.Target = null;
                process.Variables = null;
            }

            // Convert "checkout" steps into variables.
            if (process.Phases != null)
            {
                foreach (Phase phase in process.Phases)
                {
                    if (phase.Steps != null && phase.Steps.Count > 0)
                    {
                        if (phase.Steps[0] is CheckoutStep)
                        {
                            if (phase.Variables == null)
                            {
                                phase.Variables = new List<IVariable>();
                            }

                            foreach (Variable variable in (phase.Steps[0] as CheckoutStep).GetVariables(process.Resources))
                            {
                                phase.Variables.Add(variable);
                            }

                            phase.Steps.RemoveAt(0);
                        }

                        // Validate "checkout" is only used as the first step within a phase.
                        if (phase.Steps.Any(x => x is CheckoutStep))
                        {
                            throw new Exception($"Step '{YamlConstants.Checkout}' is currently only supported as the first step within a phase.");
                        }
                    }
                }
            }

            // Record all known phase names.
            var knownPhaseNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            if (process.Phases != null)
            {
                foreach (Phase phase in process.Phases)
                {
                    knownPhaseNames.Add(phase.Name);
                }
            }

            // Generate missing names.
            Int32? nextPhase = null;
            if (process.Phases != null)
            {
                foreach (Phase phase in process.Phases)
                {
                    if (String.IsNullOrEmpty(phase.Name))
                    {
                        String candidateName = String.Format(CultureInfo.InvariantCulture, "Phase{0}", nextPhase);
                        while (!knownPhaseNames.Add(candidateName))
                        {
                            nextPhase = (nextPhase ?? 1) + 1;
                            candidateName = String.Format(CultureInfo.InvariantCulture, "Phase{0}", nextPhase);
                        }

                        phase.Name = candidateName;
                    }
                }
            }

            m_trace.Verbose("{0}", new TraceObject<Process, ProcessConverter>("After resolution", process));
            return process;
        }

        private PipelineFile<TObject> LoadFile<TObject, TConverter>(String path, IDictionary<String, Object> mustacheContext, CancellationToken cancellationToken, ref Int32 fileCount)
            where TConverter : IYamlTypeConverter, new()
        {
            fileCount++;
            if (m_options.MaxFiles > 0 && fileCount > m_options.MaxFiles)
            {
                throw new FormatException(TaskResources.YamlFileCount(m_options.MaxFiles));
            }

            cancellationToken.ThrowIfCancellationRequested();
            FileData file = m_fileProvider.GetFile(path);
            String mustacheReplaced;
            StringReader reader = null;
            CancellationTokenSource mustacheCancellationTokenSource = null;
            try
            {
                // Read front-matter
                IDictionary<String, Object> frontMatter = null;
                reader = new StringReader(file.Content);
                String line = reader.ReadLine();
                if (!String.Equals(line, "---", StringComparison.Ordinal))
                {
                    // No front-matter. Reset the reader.
                    reader.Dispose();
                    reader = new StringReader(file.Content);
                }
                else
                {
                    // Deseralize front-matter.
                    cancellationToken.ThrowIfCancellationRequested();
                    StringBuilder frontMatterBuilder = new StringBuilder();
                    while (true)
                    {
                        line = reader.ReadLine();
                        if (line == null)
                        {
                            throw new FormatException(TaskResources.YamlFrontMatterNotClosed(path));
                        }
                        else if (String.Equals(line, "---", StringComparison.Ordinal))
                        {
                            break;
                        }
                        else
                        {
                            frontMatterBuilder.AppendLine(line);
                        }
                    }

                    var frontMatterDeserializer = new Deserializer();
                    try
                    {
                        frontMatter = frontMatterDeserializer.Deserialize<IDictionary<String, Object>>(frontMatterBuilder.ToString());
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException(TaskResources.YamlFrontMatterNotValid(path, ex.Message), ex);
                    }
                }

                // Merge the mustache replace context.
                frontMatter = frontMatter ?? new Dictionary<String, Object>();
                if (mustacheContext != null)
                {
                    foreach (KeyValuePair<String, Object> pair in mustacheContext)
                    {
                        frontMatter[pair.Key] = pair.Value;
                    }
                }

                // Prepare the mustache options.
                mustacheCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                // var mustacheOptions = new MustacheEvaluationOptions
                // {
                //     CancellationToken = mustacheCancellationTokenSource.Token,
                //     EncodeMethod = MustacheEncodeMethods.JsonEncode,
                //     MaxResultLength = m_options.MustacheEvaluationMaxResultLength,
                // };

                // Parse the mustache template.
                cancellationToken.ThrowIfCancellationRequested();
                var mustacheParser = new MustacheTemplateParser(useDefaultHandlebarHelpers: true, useCommonTemplateHelpers: true);
                MustacheExpression mustacheExpression = mustacheParser.Parse(template: reader.ReadToEnd());

                // Limit the mustache evaluation time.
                if (m_options.MustacheEvaluationTimeout > TimeSpan.Zero)
                {
                    mustacheCancellationTokenSource.CancelAfter(m_options.MustacheEvaluationTimeout);
                }

                try
                {
                    // Perform the mustache evaluation.
                    mustacheReplaced = mustacheExpression.Evaluate(
                        replacementObject: frontMatter,
                        additionalEvaluationData: null,
                        parentContext: null,
                        partialExpressions: null
                        //options: mustacheOptions
                        );
                }
                catch (System.OperationCanceledException ex) when (mustacheCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new System.OperationCanceledException(TaskResources.MustacheEvaluationTimeout(path, m_options.MustacheEvaluationTimeout.TotalSeconds), ex);
                }

                m_trace.Verbose("{0}", new TraceFileContent($"{file.Name} after mustache replacement", mustacheReplaced));
            }
            finally
            {
                reader?.Dispose();
                reader = null;
                mustacheCancellationTokenSource?.Dispose();
                mustacheCancellationTokenSource = null;
            }

            // Deserialize
            DeserializerBuilder deserializerBuilder = new DeserializerBuilder();
            deserializerBuilder.WithTypeConverter(new TConverter());
            Deserializer deserializer = deserializerBuilder.Build();
            TObject obj = deserializer.Deserialize<TObject>(mustacheReplaced);
            m_trace.Verbose("{0}", new TraceObject<TObject, TConverter>($"{file.Name} after deserialization ", obj));
            var result = new PipelineFile<TObject> { Name = file.Name, Directory = file.Directory, Object = obj };
            return result;
        }

        private void ResolveTemplates(Process process, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
        {
            if (process.Template != null)
            {
                // Load the template.
                String templateFilePath = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: process.Template.Name);
                PipelineFile<ProcessTemplate> templateFile = LoadFile<ProcessTemplate, ProcessTemplateConverter>(templateFilePath, process.Template.Parameters, cancellationToken, ref fileCount);
                ProcessTemplate template = templateFile.Object;

                // Resolve template references within the template.
                if (template.Phases != null)
                {
                    ResolveTemplates(template.Phases, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                }
                else if (template.Steps != null)
                {
                    ResolveTemplates(template.Steps, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                }

                // Merge the template.
                ApplyStepOverrides(process.Template, template);
                process.Phases = template.Phases;
                process.Steps = template.Steps;
                process.Resources = MergeResources(process.Resources, template.Resources);
                process.Template = null;
            }
            // Resolve nested template references.
            else if (process.Phases != null)
            {
                ResolveTemplates(process.Phases, defaultRoot, cancellationToken, ref fileCount);
            }
            else
            {
                if (process.Variables != null)
                {
                    ResolveTemplates(process.Variables, defaultRoot, cancellationToken, ref fileCount);
                }

                if (process.Steps != null)
                {
                    ResolveTemplates(process.Steps, defaultRoot, cancellationToken, ref fileCount);
                }
            }
        }

        private void ResolveTemplates(IList<IPhase> phases, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
        {
            for (int i = 0 ; i < (phases?.Count ?? 0) ; )
            {
                if (phases[i] is PhasesTemplateReference)
                {
                    // Load the template.
                    var reference = phases[i] as PhasesTemplateReference;
                    String templateFilePath = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: reference.Name);
                    PipelineFile<PhasesTemplate> templateFile = LoadFile<PhasesTemplate, PhasesTemplateConverter>(templateFilePath, reference.Parameters, cancellationToken, ref fileCount);
                    PhasesTemplate template = templateFile.Object;

                    // Resolve template references within the template.
                    if (template.Steps != null)
                    {
                        ResolveTemplates(template.Steps, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                    }

                    // Merge the template.
                    ApplyStepOverrides(reference, template);
                    phases.RemoveAt(i);
                    if (template.Phases != null)
                    {
                        foreach (IPhase phase in template.Phases)
                        {
                            phases.Insert(i, phase);
                        }

                        i += template.Phases.Count;
                    }
                    else if (template.Steps != null)
                    {
                        var newPhase = new Phase { Steps = template.Steps };
                        phases.Insert(i, newPhase);
                        i++;
                    }
                }
                else
                {
                    // Resolve nested template references.
                    var phase = phases[i] as Phase;
                    if (phase.Variables != null)
                    {
                        ResolveTemplates(phase.Variables, defaultRoot, cancellationToken, ref fileCount);
                    }

                    if (phase.Steps != null)
                    {
                        ResolveTemplates(phase.Steps, defaultRoot, cancellationToken, ref fileCount);
                    }

                    i++;
                }
            }
        }

        private void ResolveTemplates(IList<IVariable> variables, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
        {
            for (int i = 0 ; i < (variables?.Count ?? 0) ; )
            {
                if (variables[i] is VariablesTemplateReference)
                {
                    // Load the template.
                    var reference = variables[i] as VariablesTemplateReference;
                    String templateFilePath = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: reference.Name);
                    PipelineFile<VariablesTemplate> templateFile = LoadFile<VariablesTemplate, VariablesTemplateConverter>(templateFilePath, reference.Parameters, cancellationToken, ref fileCount);
                    VariablesTemplate template = templateFile.Object;

                    // Merge the template.
                    variables.RemoveAt(i);
                    if (template.Variables != null)
                    {
                        foreach (IVariable variable in template.Variables)
                        {
                            variables.Insert(i, variable);
                        }

                        i += template.Variables.Count;
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        private void ResolveTemplates(IList<IStep> steps, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
        {
            for (int i = 0 ; i < (steps?.Count ?? 0); )
            {
                if (steps[i] is StepsTemplateReference)
                {
                    // Load the template.
                    var reference = steps[i] as StepsTemplateReference;
                    String templateFilePath = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: reference.Name);
                    PipelineFile<StepsTemplate> templateFile = LoadFile<StepsTemplate, StepsTemplateConverter>(templateFilePath, reference.Parameters, cancellationToken, ref fileCount);
                    StepsTemplate template = templateFile.Object;

                    // Merge the template.
                    ApplyStepOverrides(reference.StepOverrides, template.Steps);
                    steps.RemoveAt(i);
                    if (template.Steps != null)
                    {
                        foreach (IStep step in template.Steps)
                        {
                            steps.Insert(i, step);
                        }

                        i += template.Steps.Count;
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        private static void ApplyStepOverrides(PhasesTemplateReference reference, PhasesTemplate template)
        {
            // Select by phase name.
            var byPhaseNames =
                (reference.PhaseSelectors ?? new List<PhaseSelector>(0))
                .Join(inner: (template.Phases ?? new List<IPhase>(0)).Cast<Phase>(),
                    outerKeySelector: (PhaseSelector phaseSelector) => phaseSelector.Name,
                    innerKeySelector: (Phase phase) => phase.Name,
                    resultSelector: (PhaseSelector phaseSelector, Phase phase) => new { Selector = phaseSelector, Phase = phase })
                .ToArray();

            // Apply overrides from phase selectors.
            foreach (var byPhaseName in byPhaseNames)
            {
                ApplyStepOverrides(byPhaseName.Selector.StepOverrides, byPhaseName.Phase.Steps);
            }

            // Apply unqualified overrides.
            var allStepLists =
                (template.Phases ?? new List<IPhase>(0))
                .Cast<Phase>()
                .Select((Phase phase) => phase.Steps ?? new List<IStep>(0))
                .Concat(new[] { template.Steps ?? new List<IStep>(0) })
                .ToArray();
            foreach (List<IStep> stepList in allStepLists)
            {
                ApplyStepOverrides(reference.StepOverrides, stepList);
            }
        }

        private static void ApplyStepOverrides(IDictionary<String, IList<ISimpleStep>> stepOverrides, IList<IStep> steps)
        {
            stepOverrides = stepOverrides ?? new Dictionary<String, IList<ISimpleStep>>(0);
            steps = steps ?? new List<IStep>(0);
            for (int i = 0 ; i < steps.Count ; )
            {
                if (steps[i] is StepGroup)
                {
                    var stepGroup = steps[i] as StepGroup;
                    IList<ISimpleStep> overrides;
                    if (stepOverrides.TryGetValue(stepGroup.Name, out overrides))
                    {
                        steps.RemoveAt(i);
                        overrides = overrides ?? new List<ISimpleStep>(0);
                        foreach (ISimpleStep step in overrides.Select(x => x.Clone()))
                        {
                            steps.Insert(i, step);
                        }

                        i += overrides.Count;
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        private static List<ProcessResource> MergeResources(IList<ProcessResource> overrides, IList<ProcessResource> imports)
        {
            overrides = overrides ?? new List<ProcessResource>(0);
            imports = imports ?? new List<ProcessResource>(0);
            var result = new List<ProcessResource>(overrides);
            var knownOverrides = new HashSet<String>(overrides.Select(x => x.Name));
            result.AddRange(imports.Where(x => !knownOverrides.Contains(x.Name)));
            return result;
        }

        private sealed class PipelineFile<T>
        {
            public String Name { get; set; }

            public String Directory { get; set; }

            public T Object { get; set; }
        }

        private struct TraceFileContent
        {
            public TraceFileContent(String header, String value)
            {
                m_header = header;
                m_value = value;
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine();
                result.AppendLine(String.Empty.PadRight(80, '*'));
                result.AppendLine($"* {m_header}");
                result.AppendLine(String.Empty.PadRight(80, '*'));
                result.AppendLine();
                using (StringReader reader = new StringReader(m_value))
                {
                    Int32 lineNumber = 1;
                    String line = reader.ReadLine();
                    while (line != null)
                    {
                        result.AppendLine($"{lineNumber.ToString().PadLeft(4)}: {line}");
                        line = reader.ReadLine();
                        lineNumber++;
                    }
                }

                return result.ToString();
            }

            private readonly String m_header;
            private readonly String m_value;
        }

        private struct TraceObject<TObject, TConverter>
            where TConverter : IYamlTypeConverter, new()
        {
            public TraceObject(String header, TObject value)
            {
                m_header = header;
                m_value = value;
            }

            public override String ToString()
            {
                var result = new StringBuilder();
                result.AppendLine();
                result.AppendLine(String.Empty.PadRight(80, '*'));
                result.AppendLine($"* {m_header}");
                result.AppendLine(String.Empty.PadRight(80, '*'));
                result.AppendLine();
                SerializerBuilder serializerBuilder = new SerializerBuilder();
                serializerBuilder.WithTypeConverter(new TConverter());
                Serializer serializer = serializerBuilder.Build();
                result.AppendLine(serializer.Serialize(m_value));
                return result.ToString();
            }

            private readonly String m_header;
            private readonly TObject m_value;
        }

        private readonly IFileProvider m_fileProvider;
        private readonly ParseOptions m_options;
        private readonly ITraceWriter m_trace;
    }
}
