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

                        case YamlConstants.Deployment:
                            if (phase.Target != null)
                            {
                                ValidateNull(phase.Target as QueueTarget, YamlConstants.Queue, YamlConstants.Deployment, scalar);
                                ValidateNull(phase.Target as ServerTarget, YamlConstants.Server, YamlConstants.Deployment, scalar);
                                throw new NotSupportedException("Unexpected previous target type"); // Should not reach here
                            }

                            phase.Target = ReadDeploymentTarget(parser);
                            break;

                        case YamlConstants.Queue:
                            if (phase.Target != null)
                            {
                                ValidateNull(phase.Target as DeploymentTarget, YamlConstants.Deployment, YamlConstants.Queue, scalar);
                                ValidateNull(phase.Target as ServerTarget, YamlConstants.Server, YamlConstants.Queue, scalar);
                                throw new NotSupportedException("Unexpected previous target type"); // Should not reach here
                            }

                            phase.Target = ReadQueueTarget(parser);
                            break;

                        case YamlConstants.Server:
                            if (phase.Target != null)
                            {
                                ValidateNull(phase.Target as DeploymentTarget, YamlConstants.Deployment, YamlConstants.Server, scalar);
                                ValidateNull(phase.Target as QueueTarget, YamlConstants.Queue, YamlConstants.Server, scalar);
                                throw new NotSupportedException("Unexpected previous target type"); // Should not reach here
                            }

                            phase.Target = ReadServerTarget(parser);
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

        internal static DeploymentTarget ReadDeploymentTarget(IParser parser)
        {
            // Handle the simple case "deployment: group"
            if (parser.Accept<Scalar>())
            {
                return new DeploymentTarget() { Group = ReadNonEmptyString(parser) };
            }

            var result = new DeploymentTarget();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case YamlConstants.ContinueOnError:
                        result.ContinueOnError = ReadNonEmptyString(parser);
                        break;

                    case YamlConstants.Group:
                        result.Group = ReadNonEmptyString(parser);
                        break;

                    case YamlConstants.HealthOption:
                        result.HealthOption = ReadNonEmptyString(parser);
                        break;

                    case YamlConstants.Percentage:
                        result.Percentage = ReadNonEmptyString(parser);
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

                    case YamlConstants.TimeoutInMinutes:
                        result.TimeoutInMinutes = ReadNonEmptyString(parser);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                }
            }

            return result;
        }

        internal static QueueTarget ReadQueueTarget(IParser parser)
        {
            // Handle the simple case "queue: name"
            if (parser.Accept<Scalar>())
            {
                return new QueueTarget() { Name = ReadNonEmptyString(parser) };
            }

            var result = new QueueTarget();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case YamlConstants.ContinueOnError:
                        result.ContinueOnError = ReadNonEmptyString(parser);
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

                    case YamlConstants.Matrix:
                        parser.Expect<MappingStart>();
                        result.Matrix = new Dictionary<String, IDictionary<String, String>>(StringComparer.OrdinalIgnoreCase);
                        while (parser.Allow<MappingEnd>() == null)
                        {
                            String key = ReadNonEmptyString(parser);
                            result.Matrix[key] = ReadMappingOfStringString(parser, StringComparer.OrdinalIgnoreCase);
                        }

                        break;

                    case YamlConstants.Name:
                        result.Name = ReadNonEmptyString(parser);
                        break;

                    case YamlConstants.Parallel:
                        result.Parallel = ReadNonEmptyString(parser);
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

        internal static ServerTarget ReadServerTarget(IParser parser)
        {
            // Handle the simple case "server: true"
            Scalar scalar = parser.Peek<Scalar>();
            if (scalar != null)
            {
                if (ReadBoolean(parser))
                {
                    return new ServerTarget();
                }

                return null;
            }

            var result = new ServerTarget();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                scalar = parser.Expect<Scalar>();
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

                    case YamlConstants.Parallel:
                        result.Parallel = ReadNonEmptyString(parser);
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
                    QueueTarget queueTarget = null;
                    DeploymentTarget deploymentTarget = null;
                    ServerTarget serverTarget = null;
                    if ((queueTarget = p.Target as QueueTarget) != null)
                    {
                        emitter.Emit(new Scalar(YamlConstants.Queue));

                        // Test for the simple case "queue: name".
                        if (!String.IsNullOrEmpty(queueTarget.Name) &&
                            String.IsNullOrEmpty(queueTarget.ContinueOnError) &&
                            String.IsNullOrEmpty(queueTarget.Parallel) &&
                            String.IsNullOrEmpty(queueTarget.TimeoutInMinutes) &&
                            (queueTarget.Demands == null || queueTarget.Demands.Count == 0) &&
                            (queueTarget.Matrix == null || queueTarget.Matrix.Count == 0))
                        {
                            emitter.Emit(new Scalar(queueTarget.Name));
                        }
                        else // Otherwise write the mapping.
                        {
                            emitter.Emit(new MappingStart());
                            if (!String.IsNullOrEmpty(queueTarget.Name))
                            {
                                emitter.Emit(new Scalar(YamlConstants.Name));
                                emitter.Emit(new Scalar(queueTarget.Name));
                            }

                            if (!String.IsNullOrEmpty(queueTarget.ContinueOnError))
                            {
                                emitter.Emit(new Scalar(YamlConstants.ContinueOnError));
                                emitter.Emit(new Scalar(queueTarget.ContinueOnError));
                            }

                            if (!String.IsNullOrEmpty(queueTarget.Parallel))
                            {
                                emitter.Emit(new Scalar(YamlConstants.Parallel));
                                emitter.Emit(new Scalar(queueTarget.Parallel));
                            }

                            if (!String.IsNullOrEmpty(queueTarget.TimeoutInMinutes))
                            {
                                emitter.Emit(new Scalar(YamlConstants.TimeoutInMinutes));
                                emitter.Emit(new Scalar(queueTarget.TimeoutInMinutes));
                            }

                            if (queueTarget.Demands != null && queueTarget.Demands.Count > 0)
                            {
                                emitter.Emit(new Scalar(YamlConstants.Demands));
                                if (queueTarget.Demands.Count == 1)
                                {
                                    emitter.Emit(new Scalar(queueTarget.Demands[0]));
                                }
                                else
                                {
                                    WriteSequence(emitter, queueTarget.Demands);
                                }
                            }

                            if (queueTarget.Matrix != null && queueTarget.Matrix.Count > 0)
                            {
                                emitter.Emit(new Scalar(YamlConstants.Matrix));
                                emitter.Emit(new MappingStart());
                                foreach (KeyValuePair<String, IDictionary<String, String>> pair in queueTarget.Matrix.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                                {
                                    emitter.Emit(new Scalar(pair.Key));
                                    WriteMapping(emitter, pair.Value);
                                }

                                emitter.Emit(new MappingEnd());
                            }

                            emitter.Emit(new MappingEnd());
                        }
                    }
                    else if ((deploymentTarget = p.Target as DeploymentTarget) != null)
                    {
                        emitter.Emit(new Scalar(YamlConstants.Deployment));

                        // Test for the simple case "deployment: group".
                        if (!String.IsNullOrEmpty(deploymentTarget.Group) &&
                            String.IsNullOrEmpty(deploymentTarget.ContinueOnError) &&
                            String.IsNullOrEmpty(deploymentTarget.HealthOption) &&
                            String.IsNullOrEmpty(deploymentTarget.Percentage) &&
                            String.IsNullOrEmpty(deploymentTarget.TimeoutInMinutes) &&
                            (deploymentTarget.Tags == null || deploymentTarget.Tags.Count == 0))
                        {
                            emitter.Emit(new Scalar(deploymentTarget.Group));
                        }
                        else // Otherwise write the mapping.
                        {
                            emitter.Emit(new MappingStart());
                            if (!String.IsNullOrEmpty(deploymentTarget.Group))
                            {
                                emitter.Emit(new Scalar(YamlConstants.Group));
                                emitter.Emit(new Scalar(deploymentTarget.Group));
                            }

                            if (!String.IsNullOrEmpty(deploymentTarget.ContinueOnError))
                            {
                                emitter.Emit(new Scalar(YamlConstants.ContinueOnError));
                                emitter.Emit(new Scalar(deploymentTarget.ContinueOnError));
                            }

                            if (!String.IsNullOrEmpty(deploymentTarget.HealthOption))
                            {
                                emitter.Emit(new Scalar(YamlConstants.HealthOption));
                                emitter.Emit(new Scalar(deploymentTarget.HealthOption));
                            }

                            if (!String.IsNullOrEmpty(deploymentTarget.Percentage))
                            {
                                emitter.Emit(new Scalar(YamlConstants.Percentage));
                                emitter.Emit(new Scalar(deploymentTarget.Percentage));
                            }

                            if (!String.IsNullOrEmpty(deploymentTarget.TimeoutInMinutes))
                            {
                                emitter.Emit(new Scalar(YamlConstants.TimeoutInMinutes));
                                emitter.Emit(new Scalar(deploymentTarget.TimeoutInMinutes));
                            }

                            if (deploymentTarget.Tags != null && deploymentTarget.Tags.Count > 0)
                            {
                                emitter.Emit(new Scalar(YamlConstants.Tags));
                                if (deploymentTarget.Tags.Count == 1)
                                {
                                    emitter.Emit(new Scalar(deploymentTarget.Tags[0]));
                                }
                                else
                                {
                                    WriteSequence(emitter, deploymentTarget.Tags);
                                }
                            }

                            emitter.Emit(new MappingEnd());
                        }
                    }
                    else if ((serverTarget = p.Target as ServerTarget) != null)
                    {
                        emitter.Emit(new Scalar(YamlConstants.Server));

                        // Test for the simple case "server: true".
                        if (String.IsNullOrEmpty(serverTarget.ContinueOnError) &&
                            String.IsNullOrEmpty(serverTarget.Parallel) &&
                            String.IsNullOrEmpty(serverTarget.TimeoutInMinutes) &&
                            (serverTarget.Matrix == null || serverTarget.Matrix.Count == 0))
                        {
                            emitter.Emit(new Scalar("true"));
                        }
                        else // Otherwise write the mapping.
                        {
                            emitter.Emit(new MappingStart());
                            if (!String.IsNullOrEmpty(serverTarget.ContinueOnError))
                            {
                                emitter.Emit(new Scalar(YamlConstants.ContinueOnError));
                                emitter.Emit(new Scalar(serverTarget.ContinueOnError));
                            }

                            if (!String.IsNullOrEmpty(serverTarget.Parallel))
                            {
                                emitter.Emit(new Scalar(YamlConstants.Parallel));
                                emitter.Emit(new Scalar(serverTarget.Parallel));
                            }

                            if (!String.IsNullOrEmpty(serverTarget.TimeoutInMinutes))
                            {
                                emitter.Emit(new Scalar(YamlConstants.TimeoutInMinutes));
                                emitter.Emit(new Scalar(serverTarget.TimeoutInMinutes));
                            }

                            if (serverTarget.Matrix != null && serverTarget.Matrix.Count > 0)
                            {
                                emitter.Emit(new Scalar(YamlConstants.Matrix));
                                emitter.Emit(new MappingStart());
                                foreach (KeyValuePair<String, IDictionary<String, String>> pair in serverTarget.Matrix.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                                {
                                    emitter.Emit(new Scalar(pair.Key));
                                    WriteMapping(emitter, pair.Value);
                                }

                                emitter.Emit(new MappingEnd());
                            }

                            emitter.Emit(new MappingEnd());
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"Unexpected target type: '{p.Target.GetType().FullName}'");
                    }
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
