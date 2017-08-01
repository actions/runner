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
        internal static IList<IVariable> ReadVariables(IParser parser, Boolean simpleOnly = false)
        {
            var result = new List<IVariable>();
            if (parser.Accept<MappingStart>())
            {
                // The simple syntax is:
                //   variables:
                //     var1: val1
                //     var2: val2
                foreach (KeyValuePair<String, String> pair in ReadMappingOfStringString(parser, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(new Variable() { Name = pair.Key, Value = pair.Value });
                }
            }
            else
            {
                // When a variables template is referenced, sequence syntax is required:
                //   variables:
                //     - name: var1
                //       value: val1
                //     - name: var2
                //       value: val2
                //     - template: path-to-variables-template.yml
                parser.Expect<SequenceStart>();
                while (parser.Allow<SequenceEnd>() == null)
                {
                    parser.Expect<MappingStart>();
                    Scalar scalar = parser.Expect<Scalar>();
                    if (String.Equals(scalar.Value, YamlConstants.Name, StringComparison.Ordinal))
                    {
                        var variable = new Variable { Name = ReadNonEmptyString(parser) };
                        while (parser.Allow<MappingEnd>() == null)
                        {
                            scalar = parser.Expect<Scalar>();
                            switch (scalar.Value ?? String.Empty)
                            {
                                case YamlConstants.Value:
                                    variable.Value = parser.Expect<Scalar>().Value;
                                    break;

                                default:
                                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}.");
                            }
                        }

                        result.Add(variable);
                    }
                    else if (String.Equals(scalar.Value, YamlConstants.Template, StringComparison.Ordinal))
                    {
                        if (simpleOnly)
                        {
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"A variables template cannot reference another variables '{YamlConstants.Template}'.");
                        }

                        var reference = new VariablesTemplateReference { Name = ReadNonEmptyString(parser) };
                        while (parser.Allow<MappingEnd>() == null)
                        {
                            scalar = parser.Expect<Scalar>();
                            switch (scalar.Value ?? String.Empty)
                            {
                                case YamlConstants.Parameters:
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
            }

            return result;
        }

        internal static void WriteVariables(IEmitter emitter, IList<IVariable> variables)
        {
            if (!variables.Any(x => x is VariablesTemplateReference))
            {
                emitter.Emit(new MappingStart());
                foreach (Variable variable in variables)
                {
                    emitter.Emit(new Scalar(variable.Name));
                    emitter.Emit(new Scalar(variable.Value));
                }

                emitter.Emit(new MappingEnd());
            }
            else
            {
                emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
                foreach (IVariable variable in variables)
                {
                    emitter.Emit(new MappingStart());
                    if (variable is Variable)
                    {
                        var v = variable as Variable;
                        emitter.Emit(new Scalar(YamlConstants.Name));
                        emitter.Emit(new Scalar(v.Name));
                        emitter.Emit(new Scalar(YamlConstants.Value));
                        emitter.Emit(new Scalar(v.Value));
                    }
                    else
                    {
                        var reference = variable as VariablesTemplateReference;
                        emitter.Emit(new Scalar(YamlConstants.Template));
                        emitter.Emit(new Scalar(reference.Name));
                        if (reference.Parameters != null && reference.Parameters.Count > 0)
                        {
                            emitter.Emit(new Scalar(YamlConstants.Parameters));
                            WriteMapping(emitter, reference.Parameters);
                        }
                    }

                    emitter.Emit(new MappingEnd());
                }

                emitter.Emit(new SequenceEnd());
            }
        }
    }
}
