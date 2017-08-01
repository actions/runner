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
    internal sealed class StepsTemplateConverter : IYamlTypeConverter
    {
        public Boolean Accepts(Type type)
        {
            return typeof(StepsTemplate) == type;
        }

        public Object ReadYaml(IParser parser, Type type)
        {
            var result = new StepsTemplate();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case YamlConstants.Steps:
                        result.Steps = ConverterUtil.ReadSteps(parser, simpleOnly: true);
                        break;

                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected steps template property: '{scalar.Value}'");
                }
            }

            return result;
        }

        public void WriteYaml(IEmitter emitter, Object value, Type type)
        {
            ConverterUtil.WriteStepsTemplate(emitter, value as StepsTemplate);
        }
    }
}
