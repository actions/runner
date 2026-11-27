#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Converts a YAML file into a TemplateToken
    /// </summary>
    internal sealed class YamlObjectReader : IObjectReader
    {
        internal YamlObjectReader(
            Int32? fileId,
            TextReader input,
            Boolean allowAnchors = false,
            Telemetry telemetry = null)
        {
            m_fileId = fileId;
            m_parser = new Parser(input);
            m_allowAnchors = allowAnchors;
            m_telemetry = telemetry ?? new Telemetry();
            m_events = new List<ParsingEvent>();
            m_anchors = new Dictionary<String, Int32>();
            m_replay = new Stack<YamlReplayState>();
        }

        public Boolean AllowLiteral(out LiteralToken value)
        {
            if (EvaluateCurrent() is Scalar scalar)
            {
                // Tag specified
                if (!String.IsNullOrEmpty(scalar.Tag))
                {
                    // String tag
                    if (String.Equals(scalar.Tag, c_stringTag, StringComparison.Ordinal))
                    {
                        value = new StringToken(m_fileId, scalar.Start.Line, scalar.Start.Column, scalar.Value);
                        MoveNext();
                        return true;
                    }

                    // Not plain style
                    if (scalar.Style != ScalarStyle.Plain)
                    {
                        throw new NotSupportedException($"The scalar style '{scalar.Style}' on line {scalar.Start.Line} and column {scalar.Start.Column} is not valid with the tag '{scalar.Tag}'");
                    }

                    // Boolean, Float, Integer, or Null
                    switch (scalar.Tag)
                    {
                        case c_booleanTag:
                            value = ParseBoolean(scalar);
                            break;
                        case c_floatTag:
                            value = ParseFloat(scalar);
                            break;
                        case c_integerTag:
                            value = ParseInteger(scalar);
                            break;
                        case c_nullTag:
                            value = ParseNull(scalar);
                            break;
                        default:
                            throw new NotSupportedException($"Unexpected tag '{scalar.Tag}'");
                    }

                    MoveNext();
                    return true;
                }

                // Plain style, determine type using YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
                if (scalar.Style == ScalarStyle.Plain)
                {
                    if (MatchNull(scalar, out var nullToken))
                    {
                        value = nullToken;
                    }
                    else if (MatchBoolean(scalar, out var booleanToken))
                    {
                        value = booleanToken;
                    }
                    else if (MatchInteger(scalar, out var numberToken) ||
                        MatchFloat(scalar, out numberToken))
                    {
                        value = numberToken;
                    }
                    else
                    {
                        value = new StringToken(m_fileId, scalar.Start.Line, scalar.Start.Column, scalar.Value);
                    }

                    MoveNext();
                    return true;
                }

                // Otherwise assume string
                value = new StringToken(m_fileId, scalar.Start.Line, scalar.Start.Column, scalar.Value);
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowSequenceStart(out SequenceToken value)
        {
            if (EvaluateCurrent() is SequenceStart sequenceStart)
            {
                value = new SequenceToken(m_fileId, sequenceStart.Start.Line, sequenceStart.Start.Column);
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowSequenceEnd()
        {
            if (EvaluateCurrent() is SequenceEnd)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        public Boolean AllowMappingStart(out MappingToken value)
        {
            if (EvaluateCurrent() is MappingStart mappingStart)
            {
                value = new MappingToken(m_fileId, mappingStart.Start.Line, mappingStart.Start.Column);
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowMappingEnd()
        {
            if (EvaluateCurrent() is MappingEnd)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumes the last parsing events, which are expected to be DocumentEnd and StreamEnd.
        /// </summary>
        public void ValidateEnd()
        {
            if (EvaluateCurrent() is DocumentEnd)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected document end parse event");
            }

            if (EvaluateCurrent() is StreamEnd)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected stream end parse event");
            }

            if (MoveNext())
            {
                throw new InvalidOperationException("Expected end of parse events");
            }
        }

        /// <summary>
        /// Consumes the first parsing events, which are expected to be StreamStart and DocumentStart.
        /// </summary>
        public void ValidateStart()
        {
            if (EvaluateCurrent() != null)
            {
                throw new InvalidOperationException("Unexpected parser state");
            }

            if (!MoveNext())
            {
                throw new InvalidOperationException("Expected a parse event");
            }

            if (EvaluateCurrent() is StreamStart)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected stream start parse event");
            }

            if (EvaluateCurrent() is DocumentStart)
            {
                MoveNext();
            }
            else
            {
                throw new InvalidOperationException("Expected document start parse event");
            }
        }

        private ParsingEvent EvaluateCurrent_Legacy()
        {
            if (m_current == null)
            {
                m_current = m_parser.Current;
                if (m_current != null)
                {
                    if (m_current is Scalar scalar)
                    {
                        // Verify not using anchors
                        if (scalar.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{scalar.Anchor}'");
                        }
                    }
                    else if (m_current is MappingStart mappingStart)
                    {
                        // Verify not using anchors
                        if (mappingStart.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{mappingStart.Anchor}'");
                        }
                    }
                    else if (m_current is SequenceStart sequenceStart)
                    {
                        // Verify not using anchors
                        if (sequenceStart.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{sequenceStart.Anchor}'");
                        }
                    }
                    else if (!(m_current is MappingEnd) &&
                        !(m_current is SequenceEnd) &&
                        !(m_current is DocumentStart) &&
                        !(m_current is DocumentEnd) &&
                        !(m_current is StreamStart) &&
                        !(m_current is StreamEnd))
                    {
                        throw new InvalidOperationException($"Unexpected parsing event type: {m_current.GetType().Name}");
                    }
                }
            }

            return m_current;
        }

        private ParsingEvent EvaluateCurrent()
        {
            if (!m_allowAnchors)
            {
                return EvaluateCurrent_Legacy();
            }

            return m_current;
        }

        private Boolean MoveNext_Legacy()
        {
            m_current = null;
            return m_parser.MoveNext();
        }

        private Boolean MoveNext()
        {
            if (!m_allowAnchors)
            {
                return MoveNext_Legacy();
            }

            // Replaying an anchor?
            // Adjust depth.
            // Pop if done.
            if (m_replay.Count > 0)
            {
                var replay = m_replay.Peek();

                if (m_current is Scalar)
                {
                    // Done?
                    if (replay.Depth == 0)
                    {
                        // Pop
                        m_replay.Pop();
                    }
                }
                else if (m_current is SequenceStart || m_current is MappingStart)
                {
                    // Increment depth
                    replay.Depth++;
                }
                else if (m_current is SequenceEnd || m_current is MappingEnd)
                {
                    // Decrement depth
                    replay.Depth--;

                    // Done?
                    if (replay.Depth == 0)
                    {
                        // Pop
                        m_replay.Pop();
                    }
                }
            }

            // Still replaying?
            if (m_replay.Count > 0)
            {
                var replay = m_replay.Peek();

                // Move next
                replay.Index++;

                // Store current
                m_current = m_events[replay.Index];
            }
            // Not replaying
            else
            {
                // Move next
                if (!m_parser.MoveNext())
                {
                    // Clear current
                    m_current = null;

                    // Short-circuit
                    return false;
                }

                // Store current
                m_current = m_parser.Current;

                // Store event
                m_events.Add(m_current);

                // Anchor?
                var anchor = (m_current as NodeEvent)?.Anchor;
                if (anchor != null)
                {
                    // Not allowed?
                    if (!m_allowAnchors)
                    {
                        throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{anchor}'");
                    }

                    // Validate node type
                    if (m_current is not Scalar && m_current is not MappingStart && m_current is not SequenceStart)
                    {
                        throw new InvalidOperationException($"Unexpected node type with anchor '{anchor}': {m_current.GetType().Name}");
                    }

                    // Store anchor index
                    m_anchors[anchor] = m_events.Count - 1;

                    // Count anchors
                    m_telemetry.YamlAnchors++;
                }

                // Count aliases
                if (m_current is AnchorAlias)
                {
                    m_telemetry.YamlAliases++;
                }

                // Validate node type
                if (m_current is not Scalar &&
                    m_current is not MappingStart &&
                    m_current is not MappingEnd &&
                    m_current is not SequenceStart &&
                    m_current is not SequenceEnd &&
                    m_current is not DocumentStart &&
                    m_current is not DocumentEnd &&
                    m_current is not StreamStart &&
                    m_current is not StreamEnd &&
                    m_current is not AnchorAlias)
                {
                    throw new InvalidOperationException($"Unexpected parsing event type: {m_current.GetType().Name}");
                }
            }
            
            // Alias?
            if (m_current is AnchorAlias alias)
            {
                // Anchor index
                if (!m_anchors.TryGetValue(alias.Value, out var anchorIndex))
                {
                    throw new InvalidOperationException($"Unknown anchor '{alias.Value}'");
                }

                // Move to anchor
                m_current = m_events[anchorIndex];

                // Push replay state
                m_replay.Push(new YamlReplayState { Index = anchorIndex, Depth = 0 });
            }
            
            // Max nodes traversed?
            m_numNodes++;
            if (m_numNodes > c_maxYamlNodes)
            {
                throw new InvalidOperationException("Maximum YAML nodes exceeded");
            }

            return true;
        }

        private BooleanToken ParseBoolean(Scalar scalar)
        {
            if (MatchBoolean(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_booleanTag); // throws
            return default;
        }

        private NumberToken ParseFloat(Scalar scalar)
        {
            if (MatchFloat(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_floatTag); // throws
            return default;
        }

        private NumberToken ParseInteger(Scalar scalar)
        {
            if (MatchInteger(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_integerTag); // throws
            return default;
        }

        private NullToken ParseNull(Scalar scalar)
        {
            if (MatchNull(scalar, out var token))
            {
                return token;
            }

            ThrowInvalidValue(scalar, c_nullTag); // throws
            return default;
        }

        private Boolean MatchBoolean(
            Scalar scalar,
            out BooleanToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            switch (scalar.Value ?? String.Empty)
            {
                case "true":
                case "True":
                case "TRUE":
                    value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, true);
                    return true;
                case "false":
                case "False":
                case "FALSE":
                    value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, false);
                    return true;
            }

            value = default;
            return false;
        }

        private Boolean MatchFloat(
            Scalar scalar,
            out NumberToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            var str = scalar.Value;
            if (!String.IsNullOrEmpty(str))
            {
                // Check for [-+]?(\.inf|\.Inf|\.INF)|\.nan|\.NaN|\.NAN
                switch (str)
                {
                    case ".inf":
                    case ".Inf":
                    case ".INF":
                    case "+.inf":
                    case "+.Inf":
                    case "+.INF":
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.PositiveInfinity);
                        return true;
                    case "-.inf":
                    case "-.Inf":
                    case "-.INF":
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.NegativeInfinity);
                        return true;
                    case ".nan":
                    case ".NaN":
                    case ".NAN":
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.NaN);
                        return true;
                }

                // Otherwise check [-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?

                // Skip leading sign
                var index = str[0] == '-' || str[0] == '+' ? 1 : 0;

                // Check for integer portion
                var length = str.Length;
                var hasInteger = false;
                while (index < length && str[index] >= '0' && str[index] <= '9')
                {
                    hasInteger = true;
                    index++;
                }

                // Check for decimal point
                var hasDot = false;
                if (index < length && str[index] == '.')
                {
                    hasDot = true;
                    index++;
                }

                // Check for decimal portion
                var hasDecimal = false;
                while (index < length && str[index] >= '0' && str[index] <= '9')
                {
                    hasDecimal = true;
                    index++;
                }

                // Check [-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)
                if ((hasDot && hasDecimal) || hasInteger)
                {
                    // Check for end
                    if (index == length)
                    {
                        // Try parse
                        if (Double.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue))
                        {
                            value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue);
                            return true;
                        }
                        // Otherwise exceeds range
                        else
                        {
                            ThrowInvalidValue(scalar, c_floatTag); // throws
                        }
                    }
                    // Check [eE][-+]?[0-9]
                    else if (index < length && (str[index] == 'e' || str[index] == 'E'))
                    {
                        index++;

                        // Skip sign
                        if (index < length && (str[index] == '-' || str[index] == '+'))
                        {
                            index++;
                        }

                        // Check for exponent
                        var hasExponent = false;
                        while (index < length && str[index] >= '0' && str[index] <= '9')
                        {
                            hasExponent = true;
                            index++;
                        }

                        // Check for end
                        if (hasExponent && index == length)
                        {
                            // Try parse
                            if (Double.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var doubleValue))
                            {
                                value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, (Double)doubleValue);
                                return true;
                            }
                            // Otherwise exceeds range
                            else
                            {
                                ThrowInvalidValue(scalar, c_floatTag); // throws
                            }
                        }
                    }
                }
            }

            value = default;
            return false;
        }

        private Boolean MatchInteger(
            Scalar scalar,
            out NumberToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            var str = scalar.Value;
            if (!String.IsNullOrEmpty(str))
            {
                // Check for [0-9]+
                var firstChar = str[0];
                if (firstChar >= '0' && firstChar <= '9' &&
                    str.Skip(1).All(x => x >= '0' && x <= '9'))
                {
                    // Try parse
                    if (Double.TryParse(str, NumberStyles.None, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue);
                        return true;
                    }

                    // Otherwise exceeds range
                    ThrowInvalidValue(scalar, c_integerTag); // throws
                }
                // Check for (-|+)[0-9]+
                else if ((firstChar == '-' || firstChar == '+') &&
                    str.Length > 1 &&
                    str.Skip(1).All(x => x >= '0' && x <= '9'))
                {
                    // Try parse
                    if (Double.TryParse(str, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue);
                        return true;
                    }

                    // Otherwise exceeds range
                    ThrowInvalidValue(scalar, c_integerTag); // throws
                }
                // Check for 0x[0-9a-fA-F]+
                else if (firstChar == '0' &&
                    str.Length > 2 &&
                    str[1] == 'x' &&
                    str.Skip(2).All(x => (x >= '0' && x <= '9') || (x >= 'a' && x <= 'f') || (x >= 'A' && x <= 'F')))
                {
                    // Try parse
                    if (Int32.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var integerValue))
                    {
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, integerValue);
                        return true;
                    }

                    // Otherwise exceeds range
                    ThrowInvalidValue(scalar, c_integerTag); // throws
                }
                // Check for 0o[0-9]+
                else if (firstChar == '0' &&
                    str.Length > 2 &&
                    str[1] == 'o' &&
                    str.Skip(2).All(x => x >= '0' && x <= '7'))
                {
                    // Try parse
                    var integerValue = default(Int32);
                    try
                    {
                        integerValue = Convert.ToInt32(str.Substring(2), 8);
                    }
                    // Otherwise exceeds range
                    catch (Exception)
                    {
                        ThrowInvalidValue(scalar, c_integerTag); // throws
                    }

                    value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, integerValue);
                    return true;
                }
            }

            value = default;
            return false;
        }

        private Boolean MatchNull(
            Scalar scalar,
            out NullToken value)
        {
            // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
            switch (scalar.Value ?? String.Empty)
            {
                case "":
                case "null":
                case "Null":
                case "NULL":
                case "~":
                    value = new NullToken(m_fileId, scalar.Start.Line, scalar.Start.Column);
                    return true;
            }

            value = default;
            return false;
        }

        private void ThrowInvalidValue(
            Scalar scalar,
            String tag)
        {
            throw new NotSupportedException($"The value '{scalar.Value}' on line {scalar.Start.Line} and column {scalar.Start.Column} is invalid for the type '{tag}'");
        }

        /// <summary>
        /// The maximum number of YAML nodes allowed when parsing a file. A single YAML node may be
        /// encountered multiple times due to YAML anchors.
        ///
        /// Note, depth and maximum accumulated bytes are tracked in an outer layer. The goal of this
        /// layer is to prevent YAML anchors from causing excessive node traversal.
        /// </summary>
        private const int c_maxYamlNodes = 50000;

        /// <summary>
        /// Boolean YAML tag
        /// </summary>
        private const String c_booleanTag = "tag:yaml.org,2002:bool";

        /// <summary>
        /// Float YAML tag
        /// </summary>
        private const String c_floatTag = "tag:yaml.org,2002:float";

        /// <summary>
        /// Integer YAML tag
        /// </summary>
        private const String c_integerTag = "tag:yaml.org,2002:int";

        /// <summary>
        /// Null YAML tag
        /// </summary>
        private const String c_nullTag = "tag:yaml.org,2002:null";

        /// <summary>
        /// String YAML tag
        /// </summary>
        private const String c_stringTag = "tag:yaml.org,2002:str";

        /// <summary>
        /// File ID
        /// </summary>
        private readonly Int32? m_fileId;

        /// <summary>
        /// Parser instance
        /// </summary>
        private readonly Parser m_parser;

        /// <summary>
        /// Current parsing event
        /// </summary>
        private ParsingEvent m_current;

        /// <summary>
        /// Indicates whether YAML anchors are allowed
        /// </summary>
        private readonly Boolean m_allowAnchors;

        /// <summary>
        /// Telemetry data
        /// </summary>
        private readonly Telemetry m_telemetry;

        /// <summary>
        /// Number of YAML nodes traversed
        /// </summary>
        private Int32 m_numNodes;

        /// <summary>
        /// All encountered parsing events
        /// </summary>
        private readonly List<ParsingEvent> m_events;

        /// <summary>
        /// Anchor event index map
        /// </summary>
        private readonly Dictionary<String, Int32> m_anchors;

        /// <summary>
        /// Stack of anchor replay states
        /// </summary>
        private readonly Stack<YamlReplayState> m_replay;
    }
}
