using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.TypeConverters
{
    internal static partial class ConverterUtil
    {
        internal static Boolean ReadBoolean(IParser parser)
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

        internal static void ReadExactString(IParser parser, String expected)
        {
            // todo: this could be strict instead? i.e. verify actually declared as a string and not a bool, etc.
            Scalar scalar = parser.Expect<Scalar>();
            if (!String.Equals(scalar.Value ?? String.Empty, expected ?? String.Empty, StringComparison.Ordinal))
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Expected value '{expected}'. Actual '{scalar.Value}'.");
            }
        }

        internal static Int32 ReadInt32(IParser parser)
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

        internal static String ReadNonEmptyString(IParser parser)
        {
            // todo: this could be strict instead? i.e. verify actually declared as a string and not a bool, etc.
            Scalar scalar = parser.Expect<Scalar>();
            if (String.IsNullOrEmpty(scalar.Value))
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Expected non-empty string value.");
            }

            return scalar.Value;
        }

        /// <summary>
        /// Reads a mapping(string, string) from start to end using the specified <c>StringComparer</c>.
        /// </summary>
        /// <param name="parser">The parser instance from which to read</param>
        /// <returns>A dictionary instance with the specified comparer</returns>
        internal static IDictionary<String, String> ReadMappingOfStringString(IParser parser, StringComparer comparer)
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

        internal static IDictionary<String, Object> ReadMapping(IParser parser, Int32 depth = 1)
        {
            var mappingStart = parser.Expect<MappingStart>();
            if (depth > MaxObjectDepth)
            {
                throw new SyntaxErrorException(mappingStart.Start, mappingStart.End, $"Max object depth of {MaxObjectDepth} exceeded.");
            }

            var mapping = new Dictionary<String, Object>();
            depth++; // Optimistically increment the depth to avoid addition within the loop.
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
                    value = ReadSequence(parser, depth);
                }
                else
                {
                    value = ReadMapping(parser, depth);
                }

                mapping.Add(key, value);
            }

            parser.Expect<MappingEnd>();
            return mapping;
        }

        internal static IList<String> ReadSequenceOfString(IParser parser)
        {
            parser.Expect<SequenceStart>();
            var sequence = new List<String>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                sequence.Add(parser.Expect<Scalar>().Value);
            }

            return sequence;
        }

        internal static IList<Object> ReadSequence(IParser parser, Int32 depth = 1)
        {
            var sequenceStart = parser.Expect<SequenceStart>();
            if (depth > MaxObjectDepth)
            {
                throw new SyntaxErrorException(sequenceStart.Start, sequenceStart.End, $"Max object depth of {MaxObjectDepth} exceeded.");
            }

            var sequence = new List<Object>();
            depth++; // Optimistically increment the depth to avoid addition within the loop.
            while (!parser.Accept<SequenceEnd>())
            {
                if (parser.Accept<Scalar>())
                {
                    sequence.Add(parser.Expect<Scalar>());
                }
                else if (parser.Accept<SequenceStart>())
                {
                    sequence.Add(ReadSequence(parser, depth));
                }
                else
                {
                    sequence.Add(ReadMapping(parser, depth));
                }
            }

            parser.Expect<SequenceEnd>();
            return sequence;
        }

        internal static void ValidateNull(Object prevObj, String prevName, String currName, Scalar scalar)
        {
            if (prevObj != null)
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"'{currName}' is not allowed. '{prevName}' was already specified at the same same level and is mutually exclusive.");
            }
        }

        internal static void WriteMapping(IEmitter emitter, IDictionary<String, Object> value)
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

        internal static void WriteMapping(IEmitter emitter, IDictionary<String, String> value)
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

        internal static void WriteSequence(IEmitter emitter, IEnumerable value)
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
                else if (obj is IEnumerable && !(obj is String))
                {
                    WriteSequence(emitter, obj as IEnumerable);
                }
                else
                {
                    emitter.Emit(new Scalar(String.Format(CultureInfo.InvariantCulture, "{0}", obj)));
                }
            }

            emitter.Emit(new SequenceEnd());
        }

        internal const Int32 MaxObjectDepth = 10;
    }
}
