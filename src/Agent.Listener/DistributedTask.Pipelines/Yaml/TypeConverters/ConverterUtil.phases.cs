using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.TypeConverters
{
    internal static partial class ConverterUtil
    {
        internal static IList<IPhase> ReadPhases(IParser parser, Boolean simpleOnly)
        {
            var result = new List<IPhase>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                result.Add(ReadPhase(parser, simpleOnly));
            }

            return result;
        }

        internal static IPhase ReadPhase(IParser parser, Boolean simpleOnly)
        {
            IPhase result;
            parser.Expect<MappingStart>();
            Scalar scalar = parser.Expect<Scalar>();
            if (String.Equals(scalar.Value, YamlConstants.Name, StringComparison.Ordinal))
            {
                var phase = new Phase { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case YamlConstants.DependsOn:
                            if (parser.Accept<Scalar>())
                            {
                                scalar = parser.Expect<Scalar>();
                                if (!String.IsNullOrEmpty(scalar.Value))
                                {
                                    phase.DependsOn = new List<String>();
                                    phase.DependsOn.Add(scalar.Value);
                                }
                            }
                            else
                            {
                                phase.DependsOn = ReadSequenceOfString(parser);
                            }

                            break;

                        case YamlConstants.Condition:
                            phase.Condition = ReadNonEmptyString(parser);
                            break;

                        case YamlConstants.ContinueOnError:
                            phase.ContinueOnError = ReadNonEmptyString(parser);
                            break;

                        case YamlConstants.EnableAccessToken:
                            phase.EnableAccessToken = ReadNonEmptyString(parser);
                            break;

                        case YamlConstants.Target:
                            phase.Target = ReadPhaseTarget(parser);
                            break;

                        case YamlConstants.Execution:
                            phase.Execution = ReadPhaseExecution(parser);
                            break;

                        case YamlConstants.Variables:
                            phase.Variables = ReadVariables(parser, simpleOnly: false);
                            break;

                        case YamlConstants.Steps:
                            phase.Steps = ReadSteps(parser, simpleOnly: false);
                            break;

                        default:
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected process property: '{scalar.Value}'");
                    }
                }

                result = phase;
            }
            else if (String.Equals(scalar.Value, YamlConstants.Template, StringComparison.Ordinal))
            {
                if (simpleOnly)
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"A phases template cannot reference another phases '{YamlConstants.Template}'.");
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

        internal static PhaseTarget ReadPhaseTarget(IParser parser)
        {
            var result = new PhaseTarget();
            if (parser.Accept<Scalar>())
            {
                ReadExactString(parser, YamlConstants.Server);
                result.Type = YamlConstants.Server;
            }
            else
            {
                parser.Expect<MappingStart>();
                while (parser.Allow<MappingEnd>() == null)
                {
                    Scalar scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case YamlConstants.DeploymentGroup:
                            result.Type = YamlConstants.DeploymentGroup;
                            result.Name = ReadNonEmptyString(parser);
                            break;

                        case YamlConstants.Demands:
                            if (parser.Accept<Scalar>())
                            {
                                scalar = parser.Expect<Scalar>();
                                if (!String.IsNullOrEmpty(scalar.Value))
                                {
                                    result.Demands = new List<String>();
                                    result.Demands.Add(scalar.Value);
                                }
                            }
                            else
                            {
                                result.Demands = ReadSequenceOfString(parser);
                            }

                            break;

                        case YamlConstants.HealthOption:
                            result.HealthOption = ReadNonEmptyString(parser);
                            break;

                        case YamlConstants.Percentage:
                            result.Percentage = ReadNonEmptyString(parser);
                            break;

                        case YamlConstants.Queue:
                            result.Type = YamlConstants.Queue;
                            result.Name = ReadNonEmptyString(parser);
                            break;

                        case YamlConstants.Tags:
                            if (parser.Accept<Scalar>())
                            {
                                scalar = parser.Expect<Scalar>();
                                if (!String.IsNullOrEmpty(scalar.Value))
                                {
                                    result.Tags = new List<String>();
                                    result.Tags.Add(scalar.Value);
                                }
                            }
                            else
                            {
                                result.Tags = ReadSequenceOfString(parser);
                            }

                            break;

                        default:
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                    }
                }
            }

            return result;
        }

        internal static PhaseExecution ReadPhaseExecution(IParser parser)
        {
            var result = new PhaseExecution();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case YamlConstants.ContinueOnError:
                        result.ContinueOnError = ReadNonEmptyString(parser);
                        break;

                    case YamlConstants.Matrix:
                        parser.Expect<MappingStart>();
                        result.Matrix = new Dictionary<String, IDictionary<String, String>>(StringComparer.OrdinalIgnoreCase);
                        while (parser.Allow<MappingEnd>() == null)
                        {
                            String key = ReadNonEmptyString(parser);
                            result.Matrix[key] = ReadMappingOfStringString(parser, StringComparer.OrdinalIgnoreCase);
                        }

                        break;

                    case YamlConstants.MaxConcurrency:
                        result.MaxConcurrency = ReadNonEmptyString(parser);
                        break;

                    case YamlConstants.TimeoutInMinutes:
                        result.TimeoutInMinutes = ReadNonEmptyString(parser);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                }
            }

            return result;
        }

        internal static void SetProperty(IParser parser, PhasesTemplateReference reference, Scalar scalar)
        {
            if (String.Equals(scalar.Value, YamlConstants.Phases, StringComparison.Ordinal))
            {
                parser.Expect<SequenceStart>();
                var selectors = new List<PhaseSelector>();
                while (parser.Allow<SequenceEnd>() == null)
                {
                    var selector = new PhaseSelector();
                    parser.Expect<MappingStart>();
                    ReadExactString(parser, YamlConstants.Name);
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
                SetProperty(parser, reference as StepsTemplateReference, scalar);
            }
        }

        internal static void SetProperty(IParser parser, PhaseSelector selector, Scalar scalar)
        {
            if (String.Equals(scalar.Value, YamlConstants.Steps, StringComparison.Ordinal))
            {
                selector.StepOverrides = ReadStepOverrides(parser);
            }
            else
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
            }
        }

        internal static void WritePhases(IEmitter emitter, IList<IPhase> phases)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (IPhase phase in phases)
            {
                WritePhase(emitter, phase);
            }

            emitter.Emit(new SequenceEnd());
        }

        internal static void WritePhase(IEmitter emitter, IPhase phase, Boolean noBootstrap = false)
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
                    emitter.Emit(new Scalar(YamlConstants.Template));
                    emitter.Emit(new Scalar(reference.Name));
                    if (reference.Parameters != null && reference.Parameters.Count > 0)
                    {
                        emitter.Emit(new Scalar(YamlConstants.Parameters));
                        WriteMapping(emitter, reference.Parameters);
                    }
                }

                if (reference.PhaseSelectors != null && reference.PhaseSelectors.Count > 0)
                {
                    emitter.Emit(new Scalar(YamlConstants.Phases));
                    emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
                    foreach (PhaseSelector selector in reference.PhaseSelectors)
                    {
                        emitter.Emit(new MappingStart());
                        if (!String.IsNullOrEmpty(selector.Name))
                        {
                            emitter.Emit(new Scalar(YamlConstants.Name));
                            emitter.Emit(new Scalar(selector.Name));
                        }

                        if (selector.StepOverrides != null && selector.StepOverrides.Count > 0)
                        {
                            emitter.Emit(new Scalar(YamlConstants.Steps));
                            WriteStepOverrides(emitter, selector.StepOverrides);
                        }

                        emitter.Emit(new MappingEnd());
                    }

                    emitter.Emit(new SequenceEnd());
                }

                WriteStep(emitter, reference as StepsTemplateReference, noBootstrap: true);
            }
            else
            {
                var p = phase as Phase;
                if (!noBootstrap)
                {
                    emitter.Emit(new Scalar(YamlConstants.Name));
                    emitter.Emit(new Scalar(p.Name ?? String.Empty));
                }

                if (p.DependsOn != null && p.DependsOn.Count > 0)
                {
                    emitter.Emit(new Scalar(YamlConstants.DependsOn));
                    if (p.DependsOn.Count == 1)
                    {
                        emitter.Emit(new Scalar(p.DependsOn[0]));
                    }
                    else
                    {
                        WriteSequence(emitter, p.DependsOn);
                    }
                }

                if (!String.IsNullOrEmpty(p.Condition))
                {
                    emitter.Emit(new Scalar(YamlConstants.Condition));
                    emitter.Emit(new Scalar(p.Condition));
                }

                if (!String.IsNullOrEmpty(p.ContinueOnError))
                {
                    emitter.Emit(new Scalar(YamlConstants.ContinueOnError));
                    emitter.Emit(new Scalar(p.ContinueOnError));
                }

                if (!String.IsNullOrEmpty(p.EnableAccessToken))
                {
                    emitter.Emit(new Scalar(YamlConstants.EnableAccessToken));
                    emitter.Emit(new Scalar(p.EnableAccessToken));
                }

                if (p.Target != null)
                {
                    emitter.Emit(new Scalar(YamlConstants.Target));
                    switch (p.Target.Type ?? String.Empty)
                    {
                        case "":
                        case YamlConstants.Queue:
                        case YamlConstants.DeploymentGroup:
                            emitter.Emit(new MappingStart());
                            if (!String.IsNullOrEmpty(p.Target.Type))
                            {
                                emitter.Emit(new Scalar(p.Target.Type));
                                emitter.Emit(new Scalar(p.Target.Name));
                            }

                            if (p.Target.Demands != null && p.Target.Demands.Count > 0)
                            {
                                emitter.Emit(new Scalar(YamlConstants.Demands));
                                if (p.Target.Demands.Count == 1)
                                {
                                    emitter.Emit(new Scalar(p.Target.Demands[0]));
                                }
                                else
                                {
                                    WriteSequence(emitter, p.Target.Demands);
                                }
                            }

                            if (!String.IsNullOrEmpty(p.Target.HealthOption))
                            {
                                emitter.Emit(new Scalar(YamlConstants.HealthOption));
                                emitter.Emit(new Scalar(p.Target.HealthOption));
                            }

                            if (!String.IsNullOrEmpty(p.Target.Percentage))
                            {
                                emitter.Emit(new Scalar(YamlConstants.Percentage));
                                emitter.Emit(new Scalar(p.Target.Percentage));
                            }

                            if (p.Target.Tags != null && p.Target.Tags.Count > 0)
                            {
                                emitter.Emit(new Scalar(YamlConstants.Tags));
                                if (p.Target.Tags.Count == 1)
                                {
                                    emitter.Emit(new Scalar(p.Target.Tags[0]));
                                }
                                else
                                {
                                    WriteSequence(emitter, p.Target.Tags);
                                }
                            }

                            emitter.Emit(new MappingEnd());
                            break;

                        case YamlConstants.Server:
                            emitter.Emit(new Scalar(YamlConstants.Server));
                            break;

                        default:
                            throw new NotSupportedException($"Unexpected phase target type: '{p.Target.Type}'");
                    }
                }

                if (p.Execution != null)
                {
                    emitter.Emit(new Scalar(YamlConstants.Execution));
                    emitter.Emit(new MappingStart());
                    if (!String.IsNullOrEmpty(p.Execution.ContinueOnError))
                    {
                        emitter.Emit(new Scalar(YamlConstants.ContinueOnError));
                        emitter.Emit(new Scalar(p.Execution.ContinueOnError));
                    }

                    if (!String.IsNullOrEmpty(p.Execution.MaxConcurrency))
                    {
                        emitter.Emit(new Scalar(YamlConstants.MaxConcurrency));
                        emitter.Emit(new Scalar(p.Execution.MaxConcurrency));
                    }

                    if (!String.IsNullOrEmpty(p.Execution.TimeoutInMinutes))
                    {
                        emitter.Emit(new Scalar(YamlConstants.TimeoutInMinutes));
                        emitter.Emit(new Scalar(p.Execution.TimeoutInMinutes));
                    }

                    if (p.Execution.Matrix != null && p.Execution.Matrix.Count > 0)
                    {
                        emitter.Emit(new Scalar(YamlConstants.Matrix));
                        emitter.Emit(new MappingStart());
                        foreach (KeyValuePair<String, IDictionary<String, String>> pair in p.Execution.Matrix)
                        {
                            emitter.Emit(new Scalar(pair.Key));
                            WriteMapping(emitter, pair.Value);
                        }

                        emitter.Emit(new MappingEnd());
                    }

                    emitter.Emit(new MappingEnd());
                }

                if (p.Variables != null && p.Variables.Count > 0)
                {
                    emitter.Emit(new Scalar(YamlConstants.Variables));
                    WriteVariables(emitter, p.Variables);
                }

                if (p.Steps != null && p.Steps.Count > 0)
                {
                    emitter.Emit(new Scalar(YamlConstants.Steps));
                    WriteSteps(emitter, p.Steps);
                }
            }

            if (!noBootstrap)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        internal static void WritePhasesTemplate(IEmitter emitter, PhasesTemplate template, Boolean noBootstrapper = false)
        {
            if (!noBootstrapper)
            {
                emitter.Emit(new MappingStart());
            }

            if (template.Phases != null && template.Phases.Count > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.Phases));
                WritePhases(emitter, template.Phases);
            }

            WriteStepsTemplate(emitter, template, noBootstrapper: true);

            if (!noBootstrapper)
            {
                emitter.Emit(new MappingEnd());
            }
        }
    }
}
