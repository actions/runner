// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Pipelines
// Repo 2) vsts-agent repo on GitHub under src/Agent.Listener/DistributedTask.Pipelines
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Services.WebApi;
using YamlDotNet.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines
{
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

        public Process Load(String defaultRoot, String path, IDictionary<String, Object> mustacheContext, CancellationToken cancellationToken)
        {
            Int32 fileCount = 0;

            // Load the target file.
            path = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: path);
            PipelineFile<Process> processFile = LoadFile<Process, ProcessConverter>(path, mustacheContext, cancellationToken, ref fileCount);
            Process process = processFile.Object;
            ResolveTemplates(process, defaultRoot: processFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);

            // Create implied levels for the process.
            if (process.Jobs != null)
            {
                var newPhase = new Phase { Jobs = process.Jobs, Name = process.Name };
                process.Phases = new List<IPhase>();
                process.Phases.Add(newPhase);
                process.Jobs = null;
            }
            else if (process.Steps != null)
            {
                var newJob = new Job { Steps = process.Steps, Name = process.Name, Variables = process.Variables };
                var newPhase = new Phase { Jobs = new List<IJob>() };
                newPhase.Jobs.Add(newJob);
                process.Phases = new List<IPhase>();
                process.Phases.Add(newPhase);
                process.Steps = null;
            }

            var knownPhaseNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            var knownJobNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            if (process.Phases != null)
            {
                foreach (Phase phase in process.Phases)
                {
                    // Create implied levels for the phase.
                    if (phase.Steps != null)
                    {
                        var newJob = new Job { Steps = phase.Steps };
                        phase.Jobs = new List<IJob>(new IJob[] { newJob });
                        phase.Steps = null;
                    }

                    // Record all known phase/job names.
                    knownPhaseNames.Add(phase.Name);
                    if (phase.Jobs != null)
                    {
                        foreach (Job job in phase.Jobs)
                        {
                            knownJobNames.Add(job.Name);
                        }
                    }
                }
            }

            // Generate missing names.
            Int32? nextPhase = null;
            Int32? nextJob = null;
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

                    if (phase.Jobs != null)
                    {
                        foreach (Job job in phase.Jobs)
                        {
                            if (String.IsNullOrEmpty(job.Name))
                            {
                                String candidateName = String.Format(CultureInfo.InvariantCulture, "Build{0}", nextJob);
                                while (!knownPhaseNames.Add(candidateName))
                                {
                                    nextJob = (nextJob ?? 1) + 1;
                                    candidateName = String.Format(CultureInfo.InvariantCulture, "Build{0}", nextJob);
                                }

                                job.Name = candidateName;
                            }
                        }
                    }
                }
            }

            m_trace.Verbose("{0}", new TraceObject<Process, ProcessConverter>("After resolution", process));
            return process;
        }

        private PipelineFile<TObject> LoadFile<TObject, TConverter>(String path, IDictionary<String, Object> mustacheContext, CancellationToken cancellationToken, ref Int32 fileCount)
            where TConverter : YamlTypeConverter, new()
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
                var mustacheOptions = new MustacheEvaluationOptions
                {
                    CancellationToken = mustacheCancellationTokenSource.Token,
                    EncodeMethod = MustacheEncodeMethods.JsonEncode,
                    MaxResultLength = m_options.MustacheEvaluationMaxResultLength,
                };

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
                        partialExpressions: null,
                        options: mustacheOptions);
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
                else if (template.Jobs != null)
                {
                    ResolveTemplates(template.Jobs, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                }
                else if (template.Steps != null)
                {
                    ResolveTemplates(template.Steps, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                }

                // Merge the template.
                ApplyStepOverrides(process.Template, template);
                process.Phases = template.Phases;
                process.Jobs = template.Jobs;
                process.Steps = template.Steps;
                process.Resources = MergeResources(process.Resources, template.Resources);
                process.Template = null;
            }
            // Resolve nested template references.
            else if (process.Phases != null)
            {
                ResolveTemplates(process.Phases, defaultRoot, cancellationToken, ref fileCount);
            }
            else if (process.Jobs != null)
            {
                ResolveTemplates(process.Jobs, defaultRoot, cancellationToken, ref fileCount);
            }
            else if (process.Variables != null)
            {
                ResolveTemplates(process.Variables, defaultRoot, cancellationToken, ref fileCount);
            }
            else if (process.Steps != null)
            {
                ResolveTemplates(process.Steps, defaultRoot, cancellationToken, ref fileCount);
            }
        }

        private void ResolveTemplates(List<IPhase> phases, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
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
                    if (template.Jobs != null)
                    {
                        ResolveTemplates(template.Jobs, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                    }
                    else if (template.Steps != null)
                    {
                        ResolveTemplates(template.Steps, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                    }

                    // Merge the template.
                    ApplyStepOverrides(reference, template);
                    phases.RemoveAt(i);
                    if (template.Phases != null)
                    {
                        phases.InsertRange(i, template.Phases);
                        i += template.Phases.Count;
                    }
                    else if (template.Jobs != null)
                    {
                        var newPhase = new Phase { Jobs = template.Jobs };
                        phases.Insert(i, newPhase);
                        i++;
                    }
                    else if (template.Steps != null)
                    {
                        var newJob = new Job { Steps = template.Steps };
                        var newPhase = new Phase { Jobs = new List<IJob>(new IJob[] { newJob }) };
                        phases.Insert(i, newPhase);
                        i++;
                    }
                }
                else
                {
                    // Resolve nested template references.
                    var phase = phases[i] as Phase;
                    if (phase.Jobs != null)
                    {
                        ResolveTemplates(phase.Jobs, defaultRoot, cancellationToken, ref fileCount);
                    }
                    else if (phase.Variables != null)
                    {
                        ResolveTemplates(phase.Variables, defaultRoot, cancellationToken, ref fileCount);
                    }
                    else if (phase.Steps != null)
                    {
                        ResolveTemplates(phase.Steps, defaultRoot, cancellationToken, ref fileCount);
                    }

                    i++;
                }
            }
        }

        private void ResolveTemplates(List<IJob> jobs, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
        {
            for (int i = 0 ; i < (jobs?.Count ?? 0) ; )
            {
                if (jobs[i] is JobsTemplateReference)
                {
                    // Load the template.
                    var reference = jobs[i] as JobsTemplateReference;
                    String templateFilePath = m_fileProvider.ResolvePath(defaultRoot: defaultRoot, path: reference.Name);
                    PipelineFile<JobsTemplate> templateFile = LoadFile<JobsTemplate, JobsTemplateConverter>(templateFilePath, reference.Parameters, cancellationToken, ref fileCount);
                    JobsTemplate template = templateFile.Object;

                    // Resolve template references within the template.
                    if (template.Jobs != null)
                    {
                        foreach (Job job in template.Jobs)
                        {
                            if (job.Variables != null)
                            {
                                ResolveTemplates(job.Variables, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                            }
                        }
                    }
                    else if (template.Steps != null)
                    {
                        ResolveTemplates(template.Steps, defaultRoot: templateFile.Directory, cancellationToken: cancellationToken, fileCount: ref fileCount);
                    }

                    // Merge the template.
                    ApplyStepOverrides(reference, template);
                    jobs.RemoveAt(i);
                    if (template.Jobs != null)
                    {
                        jobs.InsertRange(i, template.Jobs);
                        i += template.Jobs.Count;
                    }
                    else if (template.Steps != null)
                    {
                        var newJob = new Job { Steps = template.Steps };
                        jobs.Insert(i, newJob);
                        i++;
                    }
                }
                else
                {
                    // Resolve nested template references.
                    var job = jobs[i] as Job;
                    if (job.Variables != null)
                    {
                        ResolveTemplates(job.Variables, defaultRoot, cancellationToken, ref fileCount);
                    }

                    if (job.Steps != null)
                    {
                        ResolveTemplates(job.Steps, defaultRoot, cancellationToken, ref fileCount);
                    }

                    i++;
                }
            }
        }

        private void ResolveTemplates(List<IVariable> variables, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
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
                        variables.InsertRange(i, template.Variables);
                        i += template.Variables.Count;
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        private void ResolveTemplates(List<IStep> steps, String defaultRoot, CancellationToken cancellationToken, ref Int32 fileCount)
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
                        steps.InsertRange(i, template.Steps);
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
            foreach (var byPhaseName in byPhaseNames)
            {
                // Select by phase name + job name.
                var byPhaseNamesAndJobNames =
                    (byPhaseName.Selector.JobSelectors ?? new List<JobSelector>(0))
                    .Join(inner: (byPhaseName.Phase.Jobs ?? new List<IJob>(0)).Cast<Job>(),
                        outerKeySelector: (JobSelector jobSelector) => jobSelector.Name,
                        innerKeySelector: (Job job) => job.Name,
                        resultSelector: (JobSelector jobSelector, Job job) => new { Selector = jobSelector, Job = job })
                    .ToArray();
                foreach (var byPhaseNameAndJobName in byPhaseNamesAndJobNames)
                {
                    // Apply overrides from phase + job selectors.
                    ApplyStepOverrides(byPhaseNameAndJobName.Selector.StepOverrides, byPhaseNameAndJobName.Job.Steps);
                }
            }

            // Select by job name.
            var allJobs =
                (template.Phases ?? new List<IPhase>(0))
                .Cast<Phase>()
                .SelectMany((Phase phase) => phase.Jobs ?? new List<IJob>(0))
                .Concat(template.Jobs ?? new List<IJob>(0))
                .Cast<Job>()
                .ToArray();
            var byJobNames =
                (reference.JobSelectors ?? new List<JobSelector>(0))
                .Join(inner: allJobs,
                    outerKeySelector: (JobSelector jobSelector) => jobSelector.Name,
                    innerKeySelector: (Job job) => job.Name,
                    resultSelector: (JobSelector jobSelector, Job job) => new { Selector = jobSelector, Job = job })
                .ToArray();

            // Apply overrides from job selectors.
            foreach (var byJobName in byJobNames)
            {
                ApplyStepOverrides(byJobName.Selector.StepOverrides, byJobName.Job.Steps);
            }

            // Apply overrides from phase selectors.
            foreach (var byPhaseName in byPhaseNames)
            {
                foreach (Job job in byPhaseName.Phase.Jobs ?? new List<IJob>(0))
                {
                    ApplyStepOverrides(byPhaseName.Selector.StepOverrides, job.Steps);
                }
            }

            // Apply unqualified overrides.
            var allStepLists =
                allJobs
                .Select((Job job) => job.Steps ?? new List<IStep>(0))
                .Concat(new[] { template.Steps ?? new List<IStep>(0) })
                .ToArray();
            foreach (List<IStep> stepList in allStepLists)
            {
                ApplyStepOverrides(reference.StepOverrides, stepList);
            }
        }

        private static void ApplyStepOverrides(JobsTemplateReference reference, JobsTemplate template)
        {
            // Select by job name.
            var byJobNames =
                (reference.JobSelectors ?? new List<JobSelector>(0))
                .Join(inner: (template.Jobs ?? new List<IJob>(0)).Cast<Job>(),
                    outerKeySelector: (JobSelector jobSelector) => jobSelector.Name,
                    innerKeySelector: (Job job) => job.Name,
                    resultSelector: (JobSelector jobSelector, Job job) => new { Selector = jobSelector, Job = job })
                .ToArray();

            // Apply overrides from job selectors.
            foreach (var byJobName in byJobNames)
            {
                ApplyStepOverrides(byJobName.Selector.StepOverrides, byJobName.Job.Steps);
            }

            // Apply unqualified overrides.
            var allStepLists =
                (template.Jobs ?? new List<IJob>(0))
                .Cast<Job>()
                .Select((Job job) => job.Steps ?? new List<IStep>(0))
                .Concat(new[] { template.Steps ?? new List<IStep>(0) })
                .ToArray();
            foreach (List<IStep> stepList in allStepLists)
            {
                ApplyStepOverrides(reference.StepOverrides, stepList);
            }
        }

        private static void ApplyStepOverrides(IDictionary<String, List<ISimpleStep>> stepOverrides, List<IStep> steps)
        {
            stepOverrides = stepOverrides ?? new Dictionary<String, List<ISimpleStep>>(0);
            steps = steps ?? new List<IStep>(0);
            for (int i = 0 ; i < steps.Count ; )
            {
                if (steps[i] is StepsPhase)
                {
                    var stepsPhase = steps[i] as StepsPhase;
                    List<ISimpleStep> overrides;
                    if (stepOverrides.TryGetValue(stepsPhase.Name, out overrides))
                    {
                        steps.RemoveAt(i);
                        overrides = overrides ?? new List<ISimpleStep>(0);
                        steps.InsertRange(i, overrides.Select(x => x.Clone()));
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

        private static List<ProcessResource> MergeResources(List<ProcessResource> overrides, List<ProcessResource> imports)
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
            where TConverter : YamlTypeConverter, new()
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

    public interface ITraceWriter
    {
        void Info(String format, params Object[] args);

        void Verbose(String format, params Object[] args);
    }

    public interface IFileProvider
    {
        FileData GetFile(String path);

        String ResolvePath(String defaultRoot, String path);
    }

    public sealed class FileData
    {
        public String Name { get; set; }

        public String Directory { get; set; }

        public String Content { get; set; }
    }

    public sealed class ParseOptions
    {
        public ParseOptions()
        {
        }

        internal ParseOptions(ParseOptions copy)
        {
            MaxFiles = copy.MaxFiles;
            MustacheEvaluationMaxResultLength = copy.MustacheEvaluationMaxResultLength;
            MustacheEvaluationTimeout = copy.MustacheEvaluationTimeout;
            MustacheMaxDepth = copy.MustacheMaxDepth;
        }

        /// <summary>
        /// Gets or sets the maximum number files that can be loaded when parsing a pipeline. Zero or less is treated as infinite.
        /// </summary>
        public Int32 MaxFiles { get; set; }

        /// <summary>
        /// Gets or sets the evaluation max result bytes for each mustache template. Zero or less is treated as unlimited.
        /// </summary>
        public Int32 MustacheEvaluationMaxResultLength { get; set; }

        /// <summary>
        /// Gets or sets the evaluation timeout for each mustache template. Zero or less is treated as infinite.
        /// </summary>
        public TimeSpan MustacheEvaluationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth for each mustache template. This number limits the maximum nest level. Any number less
        /// than 1 is treated as Int32.MaxValue. An exception will be thrown when the threshold is exceeded.
        /// </summary>
        public Int32 MustacheMaxDepth { get; set; }
    }
}
