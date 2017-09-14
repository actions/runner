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
    internal sealed class ProcessTemplateConverter : IYamlTypeConverter
    {
        public Boolean Accepts(Type type)
        {
            return typeof(ProcessTemplate) == type;
        }

        public Object ReadYaml(IParser parser, Type type)
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

                    case YamlConstants.Resources:
                        result.Resources = ConverterUtil.ReadProcessResources(parser);
                        break;

                    //
                    // Phases template properties
                    //

                    case YamlConstants.Phases:
                        ConverterUtil.ValidateNull(result.Steps, YamlConstants.Steps, YamlConstants.Phases, scalar);
                        result.Phases = ConverterUtil.ReadPhases(parser, simpleOnly: false);
                        break;

                    //
                    // Steps template properties
                    //

                    case YamlConstants.Steps:
                        ConverterUtil.ValidateNull(result.Phases, YamlConstants.Phases, YamlConstants.Steps, scalar);
                        result.Steps = ConverterUtil.ReadSteps(parser, simpleOnly: false);
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
            var template = value as ProcessTemplate;
            if (template.Resources != null && template.Resources.Count > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.Resources));
                ConverterUtil.WriteProcessResources(emitter, template.Resources);
            }

            ConverterUtil.WritePhasesTemplate(emitter, template, noBootstrapper: true);
            emitter.Emit(new MappingEnd());
        }
    }
}
