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
    internal sealed class ProcessConverter : IYamlTypeConverter
    {
        public Boolean Accepts(Type type)
        {
            return typeof(Process).IsAssignableFrom(type);
        }

        public Object ReadYaml(IParser parser, Type type)
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

                    case YamlConstants.Resources:
                        result.Resources = ConverterUtil.ReadProcessResources(parser);
                        break;

                    case YamlConstants.Template:
                        ConverterUtil.ValidateNull(result.Phases, YamlConstants.Phases, YamlConstants.Template, scalar);
                        ConverterUtil.ValidateNull(result.ContinueOnError, YamlConstants.ContinueOnError, YamlConstants.Template, scalar);
                        ConverterUtil.ValidateNull(result.Target, YamlConstants.Target, YamlConstants.Template, scalar);
                        ConverterUtil.ValidateNull(result.Execution, YamlConstants.Execution, YamlConstants.Template, scalar);
                        ConverterUtil.ValidateNull(result.Variables, YamlConstants.Variables, YamlConstants.Template, scalar);
                        ConverterUtil.ValidateNull(result.Steps, YamlConstants.Steps, YamlConstants.Template, scalar);
                        result.Template = ConverterUtil.ReadProcessTemplateReference(parser);
                        break;

                    case YamlConstants.Phases:
                        ConverterUtil.ValidateNull(result.Template, YamlConstants.Template, YamlConstants.Phases, scalar);
                        ConverterUtil.ValidateNull(result.ContinueOnError, YamlConstants.ContinueOnError, YamlConstants.Phases, scalar);
                        ConverterUtil.ValidateNull(result.Target, YamlConstants.Target, YamlConstants.Phases, scalar);
                        ConverterUtil.ValidateNull(result.Execution, YamlConstants.Execution, YamlConstants.Phases, scalar);
                        ConverterUtil.ValidateNull(result.Variables, YamlConstants.Variables, YamlConstants.Phases, scalar);
                        ConverterUtil.ValidateNull(result.Steps, YamlConstants.Steps, YamlConstants.Phases, scalar);
                        result.Phases = ConverterUtil.ReadPhases(parser, simpleOnly: false);
                        break;

                    //
                    // Phase properties
                    //

                    case YamlConstants.ContinueOnError:
                        ConverterUtil.ValidateNull(result.Template, YamlConstants.Template, YamlConstants.ContinueOnError, scalar);
                        ConverterUtil.ValidateNull(result.Phases, YamlConstants.Phases, YamlConstants.ContinueOnError, scalar);
                        result.ContinueOnError = ConverterUtil.ReadNonEmptyString(parser);
                        break;

                    case YamlConstants.Target:
                        ConverterUtil.ValidateNull(result.Template, YamlConstants.Template, YamlConstants.Target, scalar);
                        ConverterUtil.ValidateNull(result.Phases, YamlConstants.Phases, YamlConstants.Target, scalar);
                        result.Target = ConverterUtil.ReadPhaseTarget(parser);
                        break;

                    case YamlConstants.Execution:
                        ConverterUtil.ValidateNull(result.Template, YamlConstants.Template, YamlConstants.Execution, scalar);
                        ConverterUtil.ValidateNull(result.Phases, YamlConstants.Phases, YamlConstants.Execution, scalar);
                        result.Execution = ConverterUtil.ReadPhaseExecution(parser);
                        break;

                    case YamlConstants.Variables:
                        ConverterUtil.ValidateNull(result.Template, YamlConstants.Template, YamlConstants.Variables, scalar);
                        ConverterUtil.ValidateNull(result.Phases, YamlConstants.Phases, YamlConstants.Variables, scalar);
                        result.Variables = ConverterUtil.ReadVariables(parser);
                        break;

                    case YamlConstants.Steps:
                        ConverterUtil.ValidateNull(result.Template, YamlConstants.Template, YamlConstants.Steps, scalar);
                        ConverterUtil.ValidateNull(result.Phases, YamlConstants.Phases, YamlConstants.Steps, scalar);
                        result.Steps = ConverterUtil.ReadSteps(parser, simpleOnly: false);
                        break;

                    //
                    // Generic properties
                    //

                    case YamlConstants.Name:
                        result.Name = scalar.Value;
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected process property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            emitter.Emit(new MappingStart());
            var process = value as Process;
            if (!String.IsNullOrEmpty(process.Name))
            {
                emitter.Emit(new Scalar(YamlConstants.Name));
                emitter.Emit(new Scalar(process.Name));
            }

            if (process.Resources != null && process.Resources.Count > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.Resources));
                ConverterUtil.WriteProcessResources(emitter, process.Resources);
            }

            if (process.Template != null)
            {
                emitter.Emit(new Scalar(YamlConstants.Template));
                emitter.Emit(new MappingStart());
                if (!String.IsNullOrEmpty(process.Template.Name))
                {
                    emitter.Emit(new Scalar(YamlConstants.Name));
                    emitter.Emit(new Scalar(process.Template.Name));
                }

                if (process.Template.Parameters != null && process.Template.Parameters.Count > 0)
                {
                    emitter.Emit(new Scalar(YamlConstants.Parameters));
                    ConverterUtil.WriteMapping(emitter, process.Template.Parameters);
                }

                ConverterUtil.WritePhase(emitter, process.Template as PhasesTemplateReference, noBootstrap: true);
                emitter.Emit(new MappingEnd());
            }

            if (process.Phases != null && process.Phases.Count > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.Phases));
                ConverterUtil.WritePhases(emitter, process.Phases);
            }

            ConverterUtil.WritePhase(emitter, process, noBootstrap: true);
            emitter.Emit(new MappingEnd());
        }
    }
}
