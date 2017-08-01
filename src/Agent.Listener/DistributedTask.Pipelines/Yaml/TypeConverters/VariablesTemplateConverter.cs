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
    internal sealed class VariablesTemplateConverter : IYamlTypeConverter
    {
        public Boolean Accepts(Type type)
        {
            return typeof(VariablesTemplate) == type;
        }

        public Object ReadYaml(IParser parser, Type type)
        {
            var result = new VariablesTemplate();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case YamlConstants.Variables:
                        result.Variables = ConverterUtil.ReadVariables(parser, simpleOnly: true);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected variables template property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            emitter.Emit(new MappingStart());
            var template = value as VariablesTemplate;
            if (template.Variables != null && template.Variables.Count > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.Variables));
                ConverterUtil.WriteVariables(emitter, template.Variables);
            }

            emitter.Emit(new MappingEnd());
        }
    }
}
