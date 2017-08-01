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
        internal static IList<ProcessResource> ReadProcessResources(IParser parser)
        {
            var result = new List<ProcessResource>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                parser.Expect<MappingStart>();
                Scalar scalar = parser.Expect<Scalar>();
                switch (scalar.Value ?? String.Empty)
                {
                    case YamlConstants.Endpoint:
                    case YamlConstants.Repo:
                        break;
                    default:
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected resource type: '{scalar.Value}'");
                }

                var resource = new ProcessResource { Type = scalar.Value };
                resource.Name = ReadNonEmptyString(parser);;
                while (parser.Allow<MappingEnd>() == null)
                {
                    string dataKey = ReadNonEmptyString(parser);
                    if (parser.Accept<MappingStart>())
                    {
                        resource.Data[dataKey] = ReadMapping(parser);
                    }
                    else if (parser.Accept<SequenceStart>())
                    {
                        resource.Data[dataKey] = ReadSequence(parser);
                    }
                    else
                    {
                        resource.Data[dataKey] = parser.Expect<Scalar>().Value ?? String.Empty;
                    }
                }

                result.Add(resource);
            }

            return result;
        }

        internal static ProcessTemplateReference ReadProcessTemplateReference(IParser parser)
        {
            parser.Expect<MappingStart>();
            ReadExactString(parser, YamlConstants.Name);
            var result = new ProcessTemplateReference { Name = ReadNonEmptyString(parser) };
            while (parser.Allow<MappingEnd>() == null)
            {
                Scalar scalar = parser.Expect<Scalar>();
                SetProperty(parser, result, scalar);
            }

            return result;
        }

        internal static void WriteProcessResources(IEmitter emitter, IList<ProcessResource> resources)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (ProcessResource resource in resources)
            {
                emitter.Emit(new MappingStart());
                emitter.Emit(new Scalar(resource.Type));
                emitter.Emit(new Scalar(resource.Name));
                if (resource.Data != null && resource.Data.Count > 0)
                {
                    foreach (KeyValuePair<String, Object> pair in resource.Data)
                    {
                        emitter.Emit(new Scalar(pair.Key));
                        if (pair.Value is String)
                        {
                            emitter.Emit(new Scalar(pair.Value as string));
                        }
                        else if (pair.Value is Dictionary<String, Object>)
                        {
                            WriteMapping(emitter, pair.Value as Dictionary<String, Object>);
                        }
                        else
                        {
                            WriteSequence(emitter, pair.Value as List<Object>);
                        }
                    }
                }

                emitter.Emit(new MappingEnd());
            }

            emitter.Emit(new SequenceEnd());
        }
    }
}
