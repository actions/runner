// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Pipelines
// Repo 2) vsts-agent repo on GitHub under src/Agent.Listener/DistributedTask.Pipelines
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines
{
    internal abstract class YamlTypeConverter : IYamlTypeConverter
    {
        public abstract Boolean Accepts(Type type);

        public abstract Object ReadYaml(IParser parser, Type type);

        public abstract void WriteYaml(IEmitter emitter, Object value, Type type);

        ////////////////////////////////////////
        // Process methods
        ////////////////////////////////////////

        protected List<ProcessResource> ReadProcessResources(IParser parser)
        {
            var result = new List<ProcessResource>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                parser.Expect<MappingStart>();
                ReadExactString(parser, PipelineConstants.Name);
                var resource = new ProcessResource { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    Scalar scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case PipelineConstants.Type:
                            resource.Type = ReadNonEmptyString(parser);
                            break;

                        case PipelineConstants.Data:
                            resource.Data = ReadMapping(parser);
                            break;

                        default:
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                    }
                }

                result.Add(resource);
            }

            return result;
        }

        protected ProcessTemplateReference ReadProcessTemplateReference(IParser parser)
        {
            parser.Expect<MappingStart>();
            ReadExactString(parser, PipelineConstants.Name);
            var result = new ProcessTemplateReference { Name = ReadNonEmptyString(parser) };
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                SetProperty(parser, result, scalar);
            }

            return result;
        }

        protected void WriteProcessResources(IEmitter emitter, List<ProcessResource> resources)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (ProcessResource resource in resources)
            {
                emitter.Emit(new MappingStart());
                emitter.Emit(new Scalar(PipelineConstants.Name));
                emitter.Emit(new Scalar(resource.Name));
                if (!String.IsNullOrEmpty(resource.Type))
                {
                    emitter.Emit(new Scalar(PipelineConstants.Type));
                    emitter.Emit(new Scalar(resource.Type));
                }

                if (resource.Data != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Data));
                    WriteMapping(emitter, resource.Data);
                }

                emitter.Emit(new MappingEnd());
            }

            emitter.Emit(new SequenceEnd());
        }

        ////////////////////////////////////////
        // Phase methods
        ////////////////////////////////////////

        protected List<IPhase> ReadPhases(IParser parser, Boolean simpleOnly)
        {
            var result = new List<IPhase>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                result.Add(ReadPhase(parser, simpleOnly));
            }

            return result;
        }

        protected IPhase ReadPhase(IParser parser, Boolean simpleOnly)
        {
            IPhase result;
            parser.Expect<MappingStart>();
            Scalar scalar = parser.Expect<Scalar>();
            if (String.Equals(scalar.Value, PipelineConstants.Name, StringComparison.Ordinal))
            {
                var phase = new Phase { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        //
                        // Phase properties
                        //

                        case PipelineConstants.Parallel:
                            ValidateNull(phase.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Parallel, scalar);
                            ValidateNull(phase.Variables, PipelineConstants.Variables, PipelineConstants.Parallel, scalar);
                            ValidateNull(phase.Steps, PipelineConstants.Steps, PipelineConstants.Parallel, scalar);
                            phase.Parallel = ReadBoolean(parser);
                            break;

                        case PipelineConstants.Target:
                            ValidateNull(phase.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Target, scalar);
                            ValidateNull(phase.Variables, PipelineConstants.Variables, PipelineConstants.Target, scalar);
                            ValidateNull(phase.Steps, PipelineConstants.Steps, PipelineConstants.Target, scalar);
                            phase.Target = ReadPhaseTarget(parser);
                            break;

                        case PipelineConstants.Jobs:
                            ValidateNull(phase.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Jobs, scalar);
                            ValidateNull(phase.Variables, PipelineConstants.Variables, PipelineConstants.Jobs, scalar);
                            ValidateNull(phase.Steps, PipelineConstants.Steps, PipelineConstants.Jobs, scalar);
                            phase.Jobs = ReadJobs(parser, simpleOnly: false);
                            break;

                        //
                        // Job properties
                        //

                        case PipelineConstants.TimeoutInMinutes:
                            ValidateNull(phase.Jobs, PipelineConstants.Jobs, PipelineConstants.TimeoutInMinutes, scalar);
                            phase.TimeoutInMinutes = ReadInt32(parser);
                            break;

                        case PipelineConstants.Variables:
                            ValidateNull(phase.Jobs, PipelineConstants.Jobs, PipelineConstants.Variables, scalar);
                            phase.Variables = ReadVariables(parser, simpleOnly: false);
                            break;

                        case PipelineConstants.Steps:
                            ValidateNull(phase.Jobs, PipelineConstants.Jobs, PipelineConstants.Steps, scalar);
                            phase.Steps = ReadSteps(parser, simpleOnly: false);
                            break;

                        default:
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected process property: '{scalar.Value}'");
                    }
                }

                result = phase;
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Template, StringComparison.Ordinal))
            {
                if (simpleOnly)
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"A phases template cannot reference another phases '{PipelineConstants.Template}'.");
                }

                var reference = new PhasesTemplateReference { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    SetProperty(parser, reference, scalar);
                }

                result = reference;
            }
            else
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unknown phase type: '{scalar.Value}'");
            }

            return result;
        }

        protected PhaseTarget ReadPhaseTarget(IParser parser)
        {
            var result = new PhaseTarget();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value)
                {
                    case PipelineConstants.Type:
                        result.Type = ReadNonEmptyString(parser);
                        break;

                    case PipelineConstants.Name:
                        result.Name = ReadNonEmptyString(parser);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                }
            }

            return result;
        }

        protected void SetProperty(IParser parser, PhasesTemplateReference reference, Scalar scalar)
        {
            if (String.Equals(scalar.Value, PipelineConstants.Phases, StringComparison.Ordinal))
            {
                parser.Expect<SequenceStart>();
                var selectors = new List<PhaseSelector>();
                while (parser.Allow<SequenceEnd>() == null)
                {
                    var selector = new PhaseSelector();
                    parser.Expect<MappingStart>();
                    ReadExactString(parser, PipelineConstants.Name);
                    selector.Name = ReadNonEmptyString(parser);
                    while (parser.Allow<MappingEnd>() == null)
                    {
                        scalar = parser.Expect<Scalar>();
                        SetProperty(parser, selector, scalar);
                    }
                }

                reference.PhaseSelectors = selectors;
            }
            else
            {
                SetProperty(parser, reference as JobsTemplateReference, scalar);
            }
        }

        protected void SetProperty(IParser parser, PhaseSelector selector, Scalar scalar)
        {
            if (String.Equals(scalar.Value, PipelineConstants.Jobs, StringComparison.Ordinal))
            {
                selector.JobSelectors = ReadJobSelectors(parser);
            }
            else
            {
                SetProperty(parser, selector as JobSelector, scalar);
            }
        }

        protected void WritePhases(IEmitter emitter, List<IPhase> phases)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (IPhase phase in phases)
            {
                WritePhase(emitter, phase);
            }

            emitter.Emit(new SequenceEnd());
        }

        protected void WritePhase(IEmitter emitter, IPhase phase, Boolean noBootstrap = false)
        {
            if (!noBootstrap)
            {
                emitter.Emit(new MappingStart());
            }

            if (phase is PhasesTemplateReference)
            {
                var reference = phase as PhasesTemplateReference;
                if (!noBootstrap)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Template));
                    emitter.Emit(new Scalar(reference.Name));
                    if (reference.Parameters != null)
                    {
                        emitter.Emit(new Scalar(PipelineConstants.Parameters));
                        WriteMapping(emitter, reference.Parameters);
                    }
                }

                if (reference.PhaseSelectors != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Phases));
                    emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
                    foreach (PhaseSelector selector in reference.PhaseSelectors)
                    {
                        emitter.Emit(new MappingStart());
                        if (!String.IsNullOrEmpty(selector.Name))
                        {
                            emitter.Emit(new Scalar(PipelineConstants.Name));
                            emitter.Emit(new Scalar(selector.Name));
                        }

                        if (selector.JobSelectors != null)
                        {
                            emitter.Emit(new Scalar(PipelineConstants.Jobs));
                            WriteJobSelectors(emitter, selector.JobSelectors);
                        }

                        if (selector.StepOverrides != null)
                        {
                            emitter.Emit(new Scalar(PipelineConstants.Steps));
                            WriteStepOverrides(emitter, selector.StepOverrides);
                        }

                        emitter.Emit(new MappingEnd());
                    }

                    emitter.Emit(new SequenceEnd());
                }

                WriteJob(emitter, reference as JobsTemplateReference, noBootstrap: true);
            }
            else
            {
                var p = phase as Phase;
                if (!noBootstrap)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Name));
                    emitter.Emit(new Scalar(p.Name ?? String.Empty));
                }

                if (p.Parallel != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Parallel));
                    emitter.Emit(new Scalar(p.Parallel.Value.ToString().ToLowerInvariant()));
                }

                if (p.Target != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Target));
                    emitter.Emit(new MappingStart());
                    if (!String.IsNullOrEmpty(p.Target.Type))
                    {
                        emitter.Emit(new Scalar(PipelineConstants.Type));
                        emitter.Emit(new Scalar(p.Target.Type));
                    }

                    if (!String.IsNullOrEmpty(p.Target.Name))
                    {
                        emitter.Emit(new Scalar(PipelineConstants.Name));
                        emitter.Emit(new Scalar(p.Target.Name));
                    }

                    emitter.Emit(new MappingEnd());
                }

                if (p.Jobs != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Jobs));
                    WriteJobs(emitter, p.Jobs);
                }

                WriteJob(emitter, p, noBootstrap: true);
            }

            if (!noBootstrap)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        protected void WritePhasesTemplate(IEmitter emitter, PhasesTemplate template, Boolean noBootstrapper = false)
        {
            if (!noBootstrapper)
            {
                emitter.Emit(new MappingStart());
            }

            if (template.Phases != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Phases));
                WritePhases(emitter, template.Phases);
            }

            WriteJobsTemplate(emitter, template, noBootstrapper: true);

            if (!noBootstrapper)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        ////////////////////////////////////////
        // Job methods
        ////////////////////////////////////////

        protected List<IJob> ReadJobs(IParser parser, Boolean simpleOnly = false)
        {
            var result = new List<IJob>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                result.Add(ReadJob(parser, simpleOnly));
            }

            return result;
        }

        protected IJob ReadJob(IParser parser, Boolean simpleOnly = false)
        {
            IJob result;
            parser.Expect<MappingStart>();
            Scalar scalar = parser.Expect<Scalar>();
            if (String.Equals(scalar.Value, PipelineConstants.Name, StringComparison.Ordinal))
            {
                var job = new Job { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case PipelineConstants.TimeoutInMinutes:
                            job.TimeoutInMinutes = ReadInt32(parser);
                            break;

                        case PipelineConstants.Variables:
                            job.Variables = ReadVariables(parser, simpleOnly: false);
                            break;

                        case PipelineConstants.Steps:
                            job.Steps = ReadSteps(parser, simpleOnly: false);
                            break;

                        default:
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}.");
                    }
                }

                result = job;
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Template, StringComparison.Ordinal))
            {
                if (simpleOnly)
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"A jobs template cannot reference another jobs '{PipelineConstants.Template}'.");
                }

                var reference = new JobsTemplateReference { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    SetProperty(parser, reference, scalar);
                }

                result = reference;
            }
            else
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unknown job type: '{scalar.Value}'");
            }

            return result;
        }

        protected List<JobSelector> ReadJobSelectors(IParser parser)
        {
            var result = new List<JobSelector>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                parser.Expect<MappingStart>();
                ReadExactString(parser, PipelineConstants.Name);
                var selector = new JobSelector { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    Scalar scalar = parser.Expect<Scalar>();
                    SetProperty(parser, selector, scalar);
                }

                result.Add(selector);
            }

            return result;
        }

        protected List<IVariable> ReadVariables(IParser parser, Boolean simpleOnly = false)
        {
            var result = new List<IVariable>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                parser.Expect<MappingStart>();
                Scalar scalar = parser.Expect<Scalar>();
                if (String.Equals(scalar.Value, PipelineConstants.Name, StringComparison.Ordinal))
                {
                    var variable = new Variable { Name = ReadNonEmptyString(parser) };
                    while (parser.Allow<MappingEnd>() == null)
                    {
                        scalar = parser.Expect<Scalar>();
                        switch (scalar.Value ?? String.Empty)
                        {
                            case PipelineConstants.Value:
                                variable.Value = parser.Expect<Scalar>().Value;
                                break;

                            case PipelineConstants.Verbatim:
                                variable.Verbatim = ReadBoolean(parser);
                                break;

                            default:
                                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}.");
                        }
                    }

                    result.Add(variable);
                }
                else if (String.Equals(scalar.Value, PipelineConstants.Template, StringComparison.Ordinal))
                {
                    if (simpleOnly)
                    {
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"A variables template cannot reference another variables '{PipelineConstants.Template}'.");
                    }

                    var reference = new VariablesTemplateReference { Name = ReadNonEmptyString(parser) };
                    while (parser.Allow<MappingEnd>() == null)
                    {
                        scalar = parser.Expect<Scalar>();
                        switch (scalar.Value ?? String.Empty)
                        {
                            case PipelineConstants.Parameters:
                                reference.Parameters = ReadMapping(parser);
                                break;

                            default:
                                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                        }
                    }

                    result.Add(reference);
                }
                else
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unknown job type: '{scalar.Value}'");
                }
            }

            return result;
        }

        protected void SetProperty(IParser parser, JobsTemplateReference reference, Scalar scalar)
        {
            switch (scalar.Value ?? String.Empty)
            {
                case PipelineConstants.Jobs:
                    reference.JobSelectors = ReadJobSelectors(parser);
                    break;

                default:
                    SetProperty(parser, reference as StepsTemplateReference, scalar);
                    break;
            }
        }

        protected void SetProperty(IParser parser, JobSelector selector, Scalar scalar)
        {
            if (String.Equals(scalar.Value, PipelineConstants.Steps, StringComparison.Ordinal))
            {
                selector.StepOverrides = ReadStepOverrides(parser);
            }
            else
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
            }
        }

        protected void WriteJobs(IEmitter emitter, List<IJob> jobs)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (IJob job in jobs)
            {
                WriteJob(emitter, job);
            }

            emitter.Emit(new SequenceEnd());
        }

        protected void WriteJob(IEmitter emitter, IJob job, Boolean noBootstrap = false)
        {
            if (!noBootstrap)
            {
                emitter.Emit(new MappingStart());
            }

            if (job is JobsTemplateReference)
            {
                var reference = job as JobsTemplateReference;
                if (!noBootstrap)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Template));
                    emitter.Emit(new Scalar(reference.Name));

                    if (reference.Parameters != null)
                    {
                        emitter.Emit(new Scalar(PipelineConstants.Parameters));
                        WriteMapping(emitter, reference.Parameters);
                    }
                }

                if (reference.JobSelectors != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Jobs));
                    WriteJobSelectors(emitter, reference.JobSelectors);
                }

                WriteStep(emitter, reference as StepsTemplateReference, noBootstrap: true);
            }
            else
            {
                var j = job as Job;
                if (!noBootstrap)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Name));
                    emitter.Emit(new Scalar(j.Name));
                }

                if (j.TimeoutInMinutes != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.TimeoutInMinutes));
                    emitter.Emit(new Scalar(string.Format(CultureInfo.InvariantCulture, "{0}", j.TimeoutInMinutes)));
                }

                if (j.Variables != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Variables));
                    WriteVariables(emitter, j.Variables);
                }

                if (j.Steps != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Steps));
                    WriteSteps(emitter, j.Steps);
                }
            }

            if (!noBootstrap)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        protected void WriteVariables(IEmitter emitter, List<IVariable> variables)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (IVariable variable in variables)
            {
                emitter.Emit(new MappingStart());
                if (variable is Variable)
                {
                    var v = variable as Variable;
                    emitter.Emit(new Scalar(PipelineConstants.Name));
                    emitter.Emit(new Scalar(v.Name));
                    emitter.Emit(new Scalar(PipelineConstants.Value));
                    emitter.Emit(new Scalar(v.Value));
                    if (v.Verbatim)
                    {
                        emitter.Emit(new Scalar(PipelineConstants.Verbatim));
                        emitter.Emit(new Scalar(v.Verbatim.ToString().ToLowerInvariant()));
                    }
                }
                else
                {
                    var reference = variable as VariablesTemplateReference;
                    emitter.Emit(new Scalar(PipelineConstants.Template));
                    emitter.Emit(new Scalar(reference.Name));
                    if (reference.Parameters != null)
                    {
                        emitter.Emit(new Scalar(PipelineConstants.Parameters));
                        WriteMapping(emitter, reference.Parameters);
                    }
                }

                emitter.Emit(new MappingEnd());
            }

            emitter.Emit(new SequenceEnd());
        }

        protected void WriteJobSelectors(IEmitter emitter, List<JobSelector> selectors)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (JobSelector selector in selectors)
            {
                emitter.Emit(new MappingStart());
                if (!String.IsNullOrEmpty(selector.Name))
                {
                    emitter.Emit(new Scalar(PipelineConstants.Name));
                    emitter.Emit(new Scalar(selector.Name));
                }

                if (selector.StepOverrides != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Steps));
                    WriteStepOverrides(emitter, selector.StepOverrides);
                }

                emitter.Emit(new MappingEnd());
            }

            emitter.Emit(new SequenceEnd());
        }

        protected void WriteJobsTemplate(IEmitter emitter, JobsTemplate template, Boolean noBootstrapper = false)
        {
            if (!noBootstrapper)
            {
                emitter.Emit(new MappingStart());
            }

            if (template.Jobs != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Jobs));
                WriteJobs(emitter, template.Jobs);
            }

            WriteStepsTemplate(emitter, template, noBootstrapper: true);

            if (!noBootstrapper)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        ////////////////////////////////////////
        // Step methods
        ////////////////////////////////////////

        protected List<IStep> ReadSteps(IParser parser, Boolean simpleOnly = false)
        {
            var result = new List<IStep>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                result.Add(ReadStep(parser, simpleOnly));
            }

            return result;
        }

        protected IStep ReadStep(IParser parser, Boolean simpleOnly = false)
        {
            IStep result;
            parser.Expect<MappingStart>();
            var scalar = parser.Expect<Scalar>();
            if (String.Equals(scalar.Value, PipelineConstants.Task, StringComparison.Ordinal))
            {
                var refString = ReadNonEmptyString(parser);
                String[] refComponents = refString.Split('@');
                var task = new TaskStep
                {
                    Enabled = true,
                    Reference = new TaskReference
                    {
                        Name = refComponents[0],
                        Version = refComponents.Length == 2 ? refComponents[1] : String.Empty,
                    },
                };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case PipelineConstants.Inputs:
                            task.Inputs = ReadMappingOfStringString(parser, StringComparer.OrdinalIgnoreCase);
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Script, StringComparison.Ordinal))
            {
                var task = new TaskStep
                {
                    Enabled = true,
                    Reference = new TaskReference
                    {
                        Name = "CmdLine",
                        Version = "2",
                    },
                    Inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase),
                };

                task.Inputs["script"] = parser.Expect<Scalar>().Value ?? String.Empty;
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case PipelineConstants.FailOnStderr:
                            task.Inputs["failOnStderr"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case PipelineConstants.WorkingDirectory:
                            task.Inputs["workingDirectory"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Bash, StringComparison.Ordinal))
            {
                var task = new TaskStep
                {
                    Enabled = true,
                    Reference = new TaskReference
                    {
                        Name = "Bash",
                        Version = "3",
                    },
                    Inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase),
                };

                task.Inputs["script"] = parser.Expect<Scalar>().Value ?? String.Empty;
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case PipelineConstants.FailOnStderr:
                            task.Inputs["failOnStderr"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case PipelineConstants.WorkingDirectory:
                            task.Inputs["workingDirectory"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, PipelineConstants.PowerShell, StringComparison.Ordinal))
            {
                var task = new TaskStep
                {
                    Enabled = true,
                    Reference = new TaskReference
                    {
                        Name = "PowerShell",
                        Version = "2",
                    },
                    Inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase),
                };

                task.Inputs["script"] = parser.Expect<Scalar>().Value ?? String.Empty;
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case PipelineConstants.ErrorActionPreference:
                            task.Inputs["errorActionPreference"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case PipelineConstants.FailOnStderr:
                            task.Inputs["failOnStderr"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case PipelineConstants.IgnoreLASTEXITCODE:
                            task.Inputs["ignoreLASTEXITCODE"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case PipelineConstants.WorkingDirectory:
                            task.Inputs["workingDirectory"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Import, StringComparison.Ordinal))
            {
                result = new ImportStep { Name = ReadNonEmptyString(parser) };
                parser.Expect<MappingEnd>();
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Export, StringComparison.Ordinal))
            {
                // todo: parse export
                result = new ExportStep();
                while (parser.Allow<MappingEnd>() == null)
                {
                    parser.MoveNext();
                }
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Phase, StringComparison.Ordinal))
            {
                if (simpleOnly)
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Steps '{PipelineConstants.Phase}' cannot be nested within a steps phase or steps template.");
                }

                var stepsPhase = new StepsPhase() { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    if (String.Equals(scalar.Value, PipelineConstants.Steps, StringComparison.Ordinal))
                    {
                        stepsPhase.Steps = ReadSteps(parser, simpleOnly: true).Cast<ISimpleStep>().ToList();
                    }
                    else
                    {
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                    }
                }

                result = stepsPhase;
            }
            else if (String.Equals(scalar.Value, PipelineConstants.Template, StringComparison.Ordinal))
            {
                if (simpleOnly)
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Steps '{PipelineConstants.Template}' cannot be nested within a steps phase or steps template.");
                }

                var templateReference = new StepsTemplateReference { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case PipelineConstants.Parameters:
                            templateReference.Parameters = ReadMapping(parser);
                            break;

                        case PipelineConstants.Steps:
                            templateReference.StepOverrides = ReadStepOverrides(parser);
                            break;

                        default:
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                    }
                }

                result = templateReference;
            }
            else
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unknown step type: '{scalar.Value}'");
            }

            return result;
        }

        protected Dictionary<String, List<ISimpleStep>> ReadStepOverrides(IParser parser)
        {
            var result = new Dictionary<String, List<ISimpleStep>>();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                String key = ReadNonEmptyString(parser);
                result[key] = ReadSteps(parser, simpleOnly: true).Cast<ISimpleStep>().ToList();
            }

            return result;
        }

        protected void SetProperty(IParser parser, StepsTemplateReference reference, Scalar scalar)
        {
            switch (scalar.Value ?? String.Empty)
            {
                case PipelineConstants.Parameters:
                    reference.Parameters = ReadMapping(parser);
                    break;

                case PipelineConstants.Steps:
                    reference.StepOverrides = ReadStepOverrides(parser);
                    break;

                default:
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
            }
        }

        protected void SetTaskControlProperty(IParser parser, TaskStep task, Scalar scalar)
        {
            switch (scalar.Value ?? String.Empty)
            {
                case PipelineConstants.Condition:
                    task.Condition = parser.Expect<Scalar>().Value;
                    break;
                case PipelineConstants.ContinueOnError:
                    task.ContinueOnError = ReadBoolean(parser);
                    break;
                case PipelineConstants.Enabled:
                    task.Enabled = ReadBoolean(parser);
                    break;
                case PipelineConstants.Environment:
                    task.Environment = ReadMappingOfStringString(parser, StringComparer.Ordinal);
                    break;
                case PipelineConstants.Name:
                    task.Name = parser.Expect<Scalar>().Value;
                    break;
                case PipelineConstants.TimeoutInMinutes:
                    task.TimeoutInMinutes = ReadInt32(parser);
                    break;
                default:
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property {scalar.Value}");
            }
        }

        protected void WriteSteps(IEmitter emitter, List<IStep> steps)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (IStep step in steps)
            {
                WriteStep(emitter, step);
            }

            emitter.Emit(new SequenceEnd());
        }

        protected void WriteStep(IEmitter emitter, IStep step, Boolean noBootstrap = false)
        {
            if (!noBootstrap)
            {
                emitter.Emit(new MappingStart());
            }

            if (step is StepsTemplateReference)
            {
                var reference = step as StepsTemplateReference;
                if (!noBootstrap)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Template));
                    emitter.Emit(new Scalar(reference.Name));

                    if (reference.Parameters != null)
                    {
                        emitter.Emit(new Scalar(PipelineConstants.Parameters));
                        WriteMapping(emitter, reference.Parameters);
                    }
                }

                if (reference.StepOverrides != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Steps));
                    WriteStepOverrides(emitter, reference.StepOverrides);
                }
            }
            else if (step is StepsPhase)
            {
                var phase = step as StepsPhase;
                emitter.Emit(new Scalar(PipelineConstants.Phase));
                emitter.Emit(new Scalar(phase.Name));
                if (phase.Steps != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Steps));
                    WriteSteps(emitter, phase.Steps.Cast<IStep>().ToList());
                }
            }
            else if (step is ImportStep)
            {
                var import = step as ImportStep;
                emitter.Emit(new Scalar(PipelineConstants.Import));
                emitter.Emit(new Scalar(import.Name));
            }
            else if (step is ExportStep)
            {
                var export = step as ExportStep;
                emitter.Emit(new Scalar(PipelineConstants.Export));
                emitter.Emit(new Scalar(export.Name));
                if (!String.IsNullOrEmpty(export.ResourceType))
                {
                    emitter.Emit(new Scalar(PipelineConstants.Type));
                    emitter.Emit(new Scalar(export.ResourceType));
                }

                if (export.Inputs != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Inputs));
                    WriteMapping(emitter, export.Inputs);
                }
            }
            else if (step is TaskStep)
            {
                var task = step as TaskStep;
                emitter.Emit(new Scalar(PipelineConstants.Task));
                if (String.IsNullOrEmpty(task.Reference.Version))
                {
                    emitter.Emit(new Scalar(task.Reference.Name));
                }
                else
                {
                    emitter.Emit(new Scalar($"{task.Reference.Name}@{task.Reference.Version}"));
                }

                if (!String.IsNullOrEmpty(task.Name))
                {
                    emitter.Emit(new Scalar(PipelineConstants.Name));
                    emitter.Emit(new Scalar(task.Name));
                }

                if (!String.IsNullOrEmpty(task.Condition))
                {
                    emitter.Emit(new Scalar(PipelineConstants.Condition));
                    emitter.Emit(new Scalar(task.Condition));
                }

                emitter.Emit(new Scalar(PipelineConstants.ContinueOnError));
                emitter.Emit(new Scalar(task.ContinueOnError.ToString().ToLowerInvariant()));
                emitter.Emit(new Scalar(PipelineConstants.Enabled));
                emitter.Emit(new Scalar(task.Enabled.ToString().ToLowerInvariant()));
                emitter.Emit(new Scalar(PipelineConstants.TimeoutInMinutes));
                emitter.Emit(new Scalar(String.Format(CultureInfo.InvariantCulture, "{0}", task.TimeoutInMinutes)));
                if (task.Inputs != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Inputs));
                    WriteMapping(emitter, task.Inputs);
                }
            }

            if (!noBootstrap)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        protected void WriteStepOverrides(IEmitter emitter, IDictionary<String, List<ISimpleStep>> overrides)
        {
            emitter.Emit(new MappingStart());
            foreach (KeyValuePair<String, List<ISimpleStep>> pair in overrides)
            {
                emitter.Emit(new Scalar(pair.Key));
                WriteSteps(emitter, pair.Value.Cast<IStep>().ToList());
            }

            emitter.Emit(new MappingEnd());
        }

        protected void WriteStepsTemplate(IEmitter emitter, StepsTemplate template, Boolean noBootstrapper = false)
        {
            if (!noBootstrapper)
            {
                emitter.Emit(new MappingStart());
            }

            if (template.Steps != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Steps));
                WriteSteps(emitter, template.Steps);
            }

            if (!noBootstrapper)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        ////////////////////////////////////////
        // General methods
        ////////////////////////////////////////

        protected Boolean ReadBoolean(IParser parser)
        {
            // todo: we may need to make this more strict, to ensure literal boolean was passed and not a string. using the style and tag, the strict determination can be made. we may also want to use 1.2 compliant boolean values, rather than 1.1.
            Scalar scalar = parser.Expect<Scalar>();
            switch ((scalar.Value ?? String.Empty).ToUpperInvariant())
            {
                case "TRUE":
                case "Y":
                case "YES":
                case "ON":
                    return true;
                case "FALSE":
                case "N":
                case "NO":
                case "OFF":
                    return false;
                default:
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Expected a boolean value. Actual: '{scalar.Value}'");
            }
        }

        protected void ReadExactString(IParser parser, String expected)
        {
            // todo: this could be strict instead? i.e. verify actually declared as a string and not a bool, etc.
            Scalar scalar = parser.Expect<Scalar>();
            if (!String.Equals(scalar.Value ?? String.Empty, expected ?? String.Empty, StringComparison.Ordinal))
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Expected value '{expected}'. Actual '{scalar.Value}'.");
            }
        }

        protected Int32 ReadInt32(IParser parser)
        {
            Scalar scalar = parser.Expect<Scalar>();
            Int32 result;
            if (!Int32.TryParse(
                scalar.Value ?? String.Empty,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out result))
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Expected an integer value. Actual: '{scalar.Value}'");
            }

            return result;
        }

        protected String ReadNonEmptyString(IParser parser)
        {
            // todo: this could be strict instead? i.e. verify actually declared as a string and not a bool, etc.
            Scalar scalar = parser.Expect<Scalar>();
            if (String.IsNullOrEmpty(scalar.Value))
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Expected non-empty string value.");
            }

            return scalar.Value;
        }

        protected TimeSpan ReadTimespan(IParser parser)
        {
            Scalar scalar = parser.Expect<Scalar>();
            TimeSpan result;
            if (!TimeSpan.TryParse(scalar.Value ?? String.Empty, CultureInfo.InvariantCulture, out result))
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Expected a timespan value. Actual: '{scalar.Value}'");
            }

            return result;
        }

        /// <summary>
        /// Reads a mapping(string, string) from start to end using the specified <c>StringComparer</c>.
        /// </summary>
        /// <param name="parser">The parser instance from which to read</param>
        /// <returns>A dictionary instance with the specified comparer</returns>
        protected Dictionary<String, String> ReadMappingOfStringString(IParser parser, StringComparer comparer)
        {
            parser.Expect<MappingStart>();
            var mappingValue = new Dictionary<String, String>(comparer);
            while (!parser.Accept<MappingEnd>())
            {
                mappingValue.Add(parser.Expect<Scalar>().Value, parser.Expect<Scalar>().Value);
            }

            parser.Expect<MappingEnd>();
            return mappingValue;
        }

        protected Dictionary<String, Object> ReadMapping(IParser parser)
        {
            parser.Expect<MappingStart>();
            var mapping = new Dictionary<String, Object>();
            while (!parser.Accept<MappingEnd>())
            {
                String key = parser.Expect<Scalar>().Value;
                Object value;
                if (parser.Accept<Scalar>())
                {
                    value = parser.Expect<Scalar>().Value;
                }
                else if (parser.Accept<SequenceStart>())
                {
                    value = ReadSequence(parser);
                }
                else
                {
                    value = ReadMapping(parser);
                }

                mapping.Add(key, value);
            }

            parser.Expect<MappingEnd>();
            return mapping;
        }

        protected List<Object> ReadSequence(IParser parser)
        {
            parser.Expect<SequenceStart>();
            var sequence = new List<Object>();
            while (!parser.Accept<SequenceEnd>())
            {
                if (parser.Accept<Scalar>())
                {
                    sequence.Add(parser.Expect<Scalar>());
                }
                else if (parser.Accept<SequenceStart>())
                {
                    sequence.Add(ReadSequence(parser));
                }
                else
                {
                    sequence.Add(ReadMapping(parser));
                }
            }

            parser.Expect<SequenceEnd>();
            return sequence;
        }

        protected void ValidateNull(Object prevObj, String prevName, String currName, Scalar scalar)
        {
            if (prevObj != null)
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"'{currName}' is not allowed. '{prevName}' was already specified at the same same level and is mutually exclusive.");
            }
        }

        protected void WriteMapping(IEmitter emitter, IDictionary<String, Object> value)
        {
            emitter.Emit(new MappingStart());
            var dictionary = value as IDictionary<String, Object>;
            foreach (KeyValuePair<String, Object> pair in dictionary)
            {
                emitter.Emit(new Scalar(pair.Key));
                if (pair.Value is IDictionary<String, Object>)
                {
                    WriteMapping(emitter, pair.Value as IDictionary<String, Object>);
                }
                else if (pair.Value is IDictionary<String, String>)
                {
                    WriteMapping(emitter, pair.Value as IDictionary<String, String>);
                }
                else if (pair.Value is IEnumerable && !(pair.Value is String))
                {
                    WriteSequence(emitter, pair.Value as IEnumerable);
                }
                else
                {
                    emitter.Emit(new Scalar(String.Format(CultureInfo.InvariantCulture, "{0}", pair.Value)));
                }
            }

            emitter.Emit(new MappingEnd());
        }

        protected void WriteMapping(IEmitter emitter, IDictionary<String, String> value)
        {
            emitter.Emit(new MappingStart());
            var dictionary = value as IDictionary<String, String>;
            foreach (KeyValuePair<String, String> pair in dictionary)
            {
                emitter.Emit(new Scalar(pair.Key));
                emitter.Emit(new Scalar(pair.Value));
            }

            emitter.Emit(new MappingEnd());
        }

        protected void WriteSequence(IEmitter emitter, IEnumerable value)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (Object obj in value)
            {
                if (obj is IDictionary<String, Object>)
                {
                    WriteMapping(emitter, obj as IDictionary<String, Object>);
                }
                else if (obj is IDictionary<String, String>)
                {
                    WriteMapping(emitter, obj as IDictionary<String, String>);
                }
                else if (value is IEnumerable && !(value is String))
                {
                    WriteSequence(emitter, obj as IEnumerable);
                }
                else
                {
                    emitter.Emit(new Scalar(String.Format(CultureInfo.InvariantCulture, "{0}", value)));
                }
            }

            emitter.Emit(new SequenceEnd());
        }
    }

    internal sealed class ProcessConverter : YamlTypeConverter
    {
        public override Boolean Accepts(Type type)
        {
            return typeof(Process).IsAssignableFrom(type);
        }

        public override Object ReadYaml(IParser parser, Type type)
        {
            var result = new Process();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    //
                    // Process properties
                    //

                    case PipelineConstants.Resources:
                        result.Resources = ReadProcessResources(parser);
                        break;

                    case PipelineConstants.Template:
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Template, scalar);
                        ValidateNull(result.Parallel, PipelineConstants.Parallel, PipelineConstants.Template, scalar);
                        ValidateNull(result.Target, PipelineConstants.Target, PipelineConstants.Template, scalar);
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Template, scalar);
                        ValidateNull(result.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Template, scalar);
                        ValidateNull(result.Variables, PipelineConstants.Variables, PipelineConstants.Template, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Template, scalar);
                        result.Template = ReadProcessTemplateReference(parser);
                        break;

                    case PipelineConstants.Phases:
                        ValidateNull(result.Template, PipelineConstants.Template, PipelineConstants.Phases, scalar);
                        ValidateNull(result.Parallel, PipelineConstants.Parallel, PipelineConstants.Phases, scalar);
                        ValidateNull(result.Target, PipelineConstants.Target, PipelineConstants.Phases, scalar);
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Phases, scalar);
                        ValidateNull(result.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Phases, scalar);
                        ValidateNull(result.Variables, PipelineConstants.Variables, PipelineConstants.Phases, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Phases, scalar);
                        result.Phases = ReadPhases(parser, simpleOnly: false);
                        break;

                    //
                    // Phase properties
                    //

                    case PipelineConstants.Parallel:
                        ValidateNull(result.Template, PipelineConstants.Template, PipelineConstants.Parallel, scalar);
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Parallel, scalar);
                        ValidateNull(result.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Parallel, scalar);
                        ValidateNull(result.Variables, PipelineConstants.Variables, PipelineConstants.Parallel, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Parallel, scalar);
                        result.Parallel = ReadBoolean(parser);
                        break;

                    case PipelineConstants.Target:
                        ValidateNull(result.Template, PipelineConstants.Template, PipelineConstants.Target, scalar);
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Target, scalar);
                        ValidateNull(result.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Target, scalar);
                        ValidateNull(result.Variables, PipelineConstants.Variables, PipelineConstants.Target, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Target, scalar);
                        result.Target = ReadPhaseTarget(parser);
                        break;

                    case PipelineConstants.Jobs:
                        ValidateNull(result.Template, PipelineConstants.Template, PipelineConstants.Target, scalar);
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Target, scalar);
                        ValidateNull(result.TimeoutInMinutes, PipelineConstants.TimeoutInMinutes, PipelineConstants.Target, scalar);
                        ValidateNull(result.Variables, PipelineConstants.Variables, PipelineConstants.Target, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Target, scalar);
                        result.Jobs = ReadJobs(parser, simpleOnly: false);
                        break;

                    //
                    // Job properties
                    //

                    case PipelineConstants.TimeoutInMinutes:
                        ValidateNull(result.Template, PipelineConstants.Template, PipelineConstants.TimeoutInMinutes, scalar);
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.TimeoutInMinutes, scalar);
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.TimeoutInMinutes, scalar);
                        result.TimeoutInMinutes = ReadInt32(parser);
                        break;

                    case PipelineConstants.Variables:
                        ValidateNull(result.Template, PipelineConstants.Template, PipelineConstants.Variables, scalar);
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Variables, scalar);
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Variables, scalar);
                        result.Variables = ReadVariables(parser);
                        break;

                    case PipelineConstants.Steps:
                        ValidateNull(result.Template, PipelineConstants.Template, PipelineConstants.Steps, scalar);
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Steps, scalar);
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Steps, scalar);
                        result.Steps = ReadSteps(parser, simpleOnly: false);
                        break;

                    //
                    // Generic properties
                    //

                    case PipelineConstants.Name:
                        result.Name = scalar.Value;
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected process property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public override void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            emitter.Emit(new MappingStart());
            var process = value as Process;
            if (!String.IsNullOrEmpty(process.Name))
            {
                emitter.Emit(new Scalar(PipelineConstants.Name));
                emitter.Emit(new Scalar(process.Name));
            }

            if (process.Resources != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Resources));
                WriteProcessResources(emitter, process.Resources);
            }

            if (process.Template != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Template));
                emitter.Emit(new MappingStart());
                if (!String.IsNullOrEmpty(process.Template.Name))
                {
                    emitter.Emit(new Scalar(PipelineConstants.Name));
                    emitter.Emit(new Scalar(process.Template.Name));
                }

                if (process.Template.Parameters != null)
                {
                    emitter.Emit(new Scalar(PipelineConstants.Parameters));
                    WriteMapping(emitter, process.Template.Parameters);
                }

                WritePhase(emitter, process.Template as PhasesTemplateReference, noBootstrap: true);
                emitter.Emit(new MappingEnd());
            }

            if (process.Phases != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Phases));
                WritePhases(emitter, process.Phases);
            }

            WritePhase(emitter, process, noBootstrap: true);
            emitter.Emit(new MappingEnd());
        }
    }

    internal sealed class ProcessTemplateConverter : YamlTypeConverter
    {
        public override Boolean Accepts(Type type)
        {
            return typeof(ProcessTemplate) == type;
        }

        public override Object ReadYaml(IParser parser, Type type)
        {
            var result = new ProcessTemplate();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    //
                    // Process template properties
                    //

                    case PipelineConstants.Resources:
                        result.Resources = ReadProcessResources(parser);
                        break;

                    //
                    // Phases template properties
                    //

                    case PipelineConstants.Phases:
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Phases, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Phases, scalar);
                        result.Phases = ReadPhases(parser, simpleOnly: false);
                        break;

                    //
                    // Jobs template properties
                    //

                    case PipelineConstants.Jobs:
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Jobs, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Jobs, scalar);
                        result.Jobs = ReadJobs(parser, simpleOnly: false);
                        break;

                    //
                    // Steps template properties
                    //

                    case PipelineConstants.Steps:
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Steps, scalar);
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Steps, scalar);
                        result.Steps = ReadSteps(parser, simpleOnly: false);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected process property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public override void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            emitter.Emit(new MappingStart());
            var template = value as ProcessTemplate;
            if (template.Resources != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Resources));
                WriteProcessResources(emitter, template.Resources);
            }

            WritePhasesTemplate(emitter, template, noBootstrapper: true);
            emitter.Emit(new MappingEnd());
        }
    }

    internal sealed class PhasesTemplateConverter : YamlTypeConverter
    {
        public override Boolean Accepts(Type type)
        {
            return typeof(PhasesTemplate) == type;
        }

        public override Object ReadYaml(IParser parser, Type type)
        {
            var result = new PhasesTemplate();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    //
                    // Phases template properties
                    //

                    case PipelineConstants.Phases:
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Phases, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Phases, scalar);
                        result.Phases = ReadPhases(parser, simpleOnly: true);
                        break;

                    //
                    // Jobs template properties
                    //

                    case PipelineConstants.Jobs:
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Jobs, scalar);
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Jobs, scalar);
                        result.Jobs = ReadJobs(parser, simpleOnly: false);
                        break;

                    //
                    // Steps template properties
                    //

                    case PipelineConstants.Steps:
                        ValidateNull(result.Phases, PipelineConstants.Phases, PipelineConstants.Steps, scalar);
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Steps, scalar);
                        result.Steps = ReadSteps(parser, simpleOnly: false);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected process property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public override void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            WritePhasesTemplate(emitter, value as PhasesTemplate);
        }
    }

    internal sealed class JobsTemplateConverter : YamlTypeConverter
    {
        public override Boolean Accepts(Type type)
        {
            return typeof(JobsTemplate) == type;
        }

        public override Object ReadYaml(IParser parser, Type type)
        {
            var result = new JobsTemplate();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    //
                    // Jobs template properties
                    //

                    case PipelineConstants.Jobs:
                        ValidateNull(result.Steps, PipelineConstants.Steps, PipelineConstants.Jobs, scalar);
                        result.Jobs = ReadJobs(parser, simpleOnly: true);
                        break;

                    //
                    // Steps template properties
                    //

                    case PipelineConstants.Steps:
                        ValidateNull(result.Jobs, PipelineConstants.Jobs, PipelineConstants.Steps, scalar);
                        result.Steps = ReadSteps(parser, simpleOnly: false);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected process property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public override void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            WriteJobsTemplate(emitter, value as JobsTemplate);
        }
    }

    internal sealed class VariablesTemplateConverter : YamlTypeConverter
    {
        public override Boolean Accepts(Type type)
        {
            return typeof(VariablesTemplate) == type;
        }

        public override Object ReadYaml(IParser parser, Type type)
        {
            var result = new VariablesTemplate();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case PipelineConstants.Variables:
                        result.Variables = ReadVariables(parser, simpleOnly: true);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected variables template property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public override void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            emitter.Emit(new MappingStart());
            var template = value as VariablesTemplate;
            if (template.Variables != null)
            {
                emitter.Emit(new Scalar(PipelineConstants.Variables));
                WriteVariables(emitter, template.Variables);
            }

            emitter.Emit(new MappingEnd());
        }
    }

    internal sealed class StepsTemplateConverter : YamlTypeConverter
    {
        public override Boolean Accepts(Type type)
        {
            return typeof(StepsTemplate) == type;
        }

        public override Object ReadYaml(IParser parser, Type type)
        {
            var result = new StepsTemplate();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case PipelineConstants.Steps:
                        result.Steps = ReadSteps(parser, simpleOnly: true);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected steps template property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public override void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            WriteStepsTemplate(emitter, value as StepsTemplate);
        }
    }
}
