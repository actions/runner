using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    internal sealed class JsonObjectReader : IObjectReader
    {
        internal JsonObjectReader(
            Int32? fileId,
            String input)
        {
            m_fileId = fileId;
            var token = JToken.Parse(input);
            m_enumerator = GetEvents(token, true).GetEnumerator();
            m_enumerator.MoveNext();
        }

        public Boolean AllowLiteral(out LiteralToken literal)
        {
            var current = m_enumerator.Current;
            switch (current.Type)
            {
                case ParseEventType.Null:
                    literal = new NullToken(m_fileId, current.Line, current.Column);
                    m_enumerator.MoveNext();
                    return true;

                case ParseEventType.Boolean:
                    literal = new BooleanToken(m_fileId, current.Line, current.Column, (Boolean)current.Value);
                    m_enumerator.MoveNext();
                    return true;

                case ParseEventType.Number:
                    literal = new NumberToken(m_fileId, current.Line, current.Column, (Double)current.Value);
                    m_enumerator.MoveNext();
                    return true;

                case ParseEventType.String:
                    literal = new StringToken(m_fileId, current.Line, current.Column, (String)current.Value);
                    m_enumerator.MoveNext();
                    return true;
            }

            literal = null;
            return false;
        }

        public Boolean AllowSequenceStart(out SequenceToken sequence)
        {
            var current = m_enumerator.Current;
            if (current.Type == ParseEventType.SequenceStart)
            {
                sequence = new SequenceToken(m_fileId, current.Line, current.Column);
                m_enumerator.MoveNext();
                return true;
            }

            sequence = null;
            return false;
        }

        public Boolean AllowSequenceEnd()
        {
            if (m_enumerator.Current.Type == ParseEventType.SequenceEnd)
            {
                m_enumerator.MoveNext();
                return true;
            }

            return false;
        }

        public Boolean AllowMappingStart(out MappingToken mapping)
        {
            var current = m_enumerator.Current;
            if (current.Type == ParseEventType.MappingStart)
            {
                mapping = new MappingToken(m_fileId, current.Line, current.Column);
                m_enumerator.MoveNext();
                return true;
            }

            mapping = null;
            return false;
        }

        public Boolean AllowMappingEnd()
        {
            if (m_enumerator.Current.Type == ParseEventType.MappingEnd)
            {
                m_enumerator.MoveNext();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumes the last parsing events, which are expected to be DocumentEnd and StreamEnd.
        /// </summary>
        public void ValidateEnd()
        {
            if (m_enumerator.Current.Type == ParseEventType.DocumentEnd)
            {
                m_enumerator.MoveNext();
                return;
            }

            throw new InvalidOperationException("Expected end of reader");
        }

        /// <summary>
        /// Consumes the first parsing events, which are expected to be StreamStart and DocumentStart.
        /// </summary>
        public void ValidateStart()
        {
            if (m_enumerator.Current.Type == ParseEventType.DocumentStart)
            {
                m_enumerator.MoveNext();
                return;
            }

            throw new InvalidOperationException("Expected start of reader");
        }

        private IEnumerable<ParseEvent> GetEvents(
            JToken token,
            Boolean root = false)
        {
            if (root)
            {
                yield return new ParseEvent(0, 0, ParseEventType.DocumentStart);
            }

            var lineInfo = token as Newtonsoft.Json.IJsonLineInfo;
            var line = lineInfo.LineNumber;
            var column = lineInfo.LinePosition;

            switch (token.Type)
            {
                case JTokenType.Null:
                    yield return new ParseEvent(line, column, ParseEventType.Null, null);
                    break;

                case JTokenType.Boolean:
                    yield return new ParseEvent(line, column, ParseEventType.Boolean, token.ToObject<Boolean>());
                    break;

                case JTokenType.Float:
                case JTokenType.Integer:
                    yield return new ParseEvent(line, column, ParseEventType.Number, token.ToObject<Double>());
                    break;

                case JTokenType.String:
                    yield return new ParseEvent(line, column, ParseEventType.String, token.ToObject<String>());
                    break;

                case JTokenType.Array:
                    yield return new ParseEvent(line, column, ParseEventType.SequenceStart);
                    foreach (var item in (token as JArray))
                    {
                        foreach (var e in GetEvents(item))
                        {
                            yield return e;
                        }
                    }
                    yield return new ParseEvent(line, column, ParseEventType.SequenceEnd);
                    break;

                case JTokenType.Object:
                    yield return new ParseEvent(line, column, ParseEventType.MappingStart);
                    foreach (var pair in (token as JObject))
                    {
                        yield return new ParseEvent(line, column, ParseEventType.String, pair.Key ?? String.Empty);
                        foreach (var e in GetEvents(pair.Value))
                        {
                            yield return e;
                        }
                    }
                    yield return new ParseEvent(line, column, ParseEventType.MappingEnd);
                    break;

                default:
                    throw new NotSupportedException($"Unexpected JTokenType {token.Type}");
            }

            if (root)
            {
                yield return new ParseEvent(0, 0, ParseEventType.DocumentEnd);
            }
        }

        private struct ParseEvent
        {
            public ParseEvent(
                Int32 line,
                Int32 column,
                ParseEventType type,
                Object value = null)
            {
                Line = line;
                Column = column;
                Type = type;
                Value = value;
            }

            public readonly Int32 Line;
            public readonly Int32 Column;
            public readonly ParseEventType Type;
            public readonly Object Value;
        }

        private enum ParseEventType
        {
            None = 0,
            Null,
            Boolean,
            Number,
            String,
            SequenceStart,
            SequenceEnd,
            MappingStart,
            MappingEnd,
            DocumentStart,
            DocumentEnd,
        }

        private IEnumerator<ParseEvent> m_enumerator;
        private Int32? m_fileId;
    }
}
