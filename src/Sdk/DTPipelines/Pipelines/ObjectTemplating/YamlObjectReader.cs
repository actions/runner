using System;
using System.Globalization;
using System.IO;
using System.Linq;

using GitHub.DistributedTask.Expressions2.Tokens;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

using Runner.Server.Azure.Devops;


using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    /// <summary>
    /// Converts a YAML file into a TemplateToken
    /// </summary>
    internal sealed class YamlObjectReader : IObjectReader
    {
        internal YamlObjectReader(
            Int32? fileId,
            TextReader input,
            bool yamlAnchors = false,
            bool yamlFold = false,
            bool yamlMerge = false,
            bool preserveString = false,
            bool forceAzurePipelines = false)
        {
            m_fileId = fileId;
            m_parser = yamlAnchors ? (IParser) new YamlAnchorParser(new Parser(input), yamlMerge) : new Parser(input);
            m_yamlFold = yamlFold;
            m_preserve_string = preserveString;
            m_force_azure_pipelines = forceAzurePipelines;
        }

        internal YamlObjectReader(
            Int32? fileId,
            string input,
            bool yamlAnchors = false,
            bool yamlFold = false,
            bool yamlMerge = false,
            bool preserveString = false,
            bool forceAzurePipelines = false) : this(fileId, new StringReader(input), yamlAnchors, yamlFold, yamlMerge, preserveString, forceAzurePipelines)
        {
            m_rawInput = input;
        }

        private string GetScalarStringValue(Scalar scalar) {
            return m_yamlFold && scalar.Style == ScalarStyle.Folded ? scalar.Value.Replace("\n", " ") : scalar.Value;
        }

        public Boolean AllowLiteral(out LiteralToken value)
        {
            if (EvaluateCurrent() is Scalar scalar)
            {
                // Tag specified
                if (scalar.Tag != null)
                {
                    // String tag
                    if (String.Equals(scalar.Tag.Value, c_stringTag, StringComparison.Ordinal))
                    {
                        value = CreateStringToken(scalar);
                        MoveNext();
                        return true;
                    }

                    // Not plain style
                    if (scalar.Style != ScalarStyle.Plain)
                    {
                        throw new NotSupportedException($"The scalar style '{scalar.Style}' on line {scalar.Start.Line} and column {scalar.Start.Column} is not valid with the tag '{scalar.Tag}'");
                    }

                    // Boolean, Float, Integer, or Null
                    switch (scalar.Tag.Value)
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

                    if(!string.IsNullOrEmpty(m_rawInput)) {
                        FillPreWhitespace(scalar, value);
                        FillPostWhitespace(scalar, value);
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
                        value = CreateStringToken(scalar);
                    }
                    if(!string.IsNullOrEmpty(m_rawInput) && value.Type != TokenType.String) {
                        FillPreWhitespace(scalar, value);
                        FillPostWhitespace(scalar, value);
                    }

                    MoveNext();
                    return true;
                }

                // Otherwise assume string
                value = CreateStringToken(scalar);
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        private LiteralToken CreateStringToken(Scalar scalar)
        {
            var tkn = new StringToken(m_fileId, scalar.Start.Line, scalar.Start.Column, GetScalarStringValue(scalar));
            if(!string.IsNullOrEmpty(m_rawInput))
            {
                tkn.RawData = m_rawInput.Substring(scalar.Start.Index, scalar.End.Index - scalar.Start.Index);
                if(scalar.Style != ScalarStyle.SingleQuoted && scalar.Style != ScalarStyle.DoubleQuoted) {
                    FillPreWhitespace(scalar, tkn);
                    // TODO Yaml scalar keys break intellisense, this modifies preprocessing
                    if(tkn.PreWhiteSpace != null && tkn.PreWhiteSpace.Line < tkn.Line) {
                        tkn.PreWhiteSpace.Line = tkn.Line.Value;
                        tkn.PreWhiteSpace.Character = 1;
                    }
                    FillPostWhitespace(scalar, tkn);
                } else {
                    tkn.PostWhiteSpace = new Position { Line = scalar.End.Line, Character = scalar.End.Column };
                }
            }
            return tkn;
        }

        private void FillPreWhitespace(NodeEvent scalar, TemplateToken tkn)
        {
            var lines = 0;
            var column = 0;
            int i = scalar.Start.Index - 1;
            for (; i >= 0 && (m_rawInput[i] == ' ' || m_rawInput[i] == '\n'); i--)
            {
                switch (m_rawInput[i])
                {
                    case ' ':
                        column++;
                        break;
                    case '\n':
                        lines++;
                        column = 0;
                        if (i - 1 >= 0 && m_rawInput[i - 1] == '\r')
                        {
                            i--;
                        }
                        break;
                }
            }
            int cpos = 0;
            if (lines > 0 && i >= 0)
            {
                int lstart = m_rawInput.LastIndexOf('\n', i);
                cpos = i + 1 - lstart;
            }
            tkn.PreWhiteSpace = new Position() { Line = scalar.Start.Line - lines, Character = lines == 0 ? scalar.Start.Column - column : cpos };
        }

        private void FillPostWhitespace(ParsingEvent scalar, TemplateToken tkn)
        {
            var lines = 0;
            var column = 0;
            int i = scalar.End.Index;
            for (; i < m_rawInput.Length && (m_rawInput[i] == ' ' || m_rawInput[i] == '\r' || m_rawInput[i] == '\n'); i++)
            {
                switch (m_rawInput[i])
                {
                    case ' ':
                        column++;
                        break;
                    case '\n':
                        lines++;
                        column = 1;
                        break;
                    case '\r':
                        lines++;
                        column = 1;
                        if (i + 1 < m_rawInput.Length && m_rawInput[i + 1] == '\n')
                        {
                            i++;
                        }
                        break;
                }
            }
            tkn.PostWhiteSpace = new Position() { Line = scalar.End.Line + lines, Character = lines == 0 ? scalar.End.Column + column : column };
        }


        public Boolean AllowSequenceStart(out SequenceToken value)
        {
            if (EvaluateCurrent() is SequenceStart sequenceStart)
            {
                value = new SequenceToken(m_fileId, sequenceStart.Start.Line, sequenceStart.Start.Column);
                if(!string.IsNullOrEmpty(m_rawInput))
                {
                    if(sequenceStart.Style == SequenceStyle.Block) {
                        FillPreWhitespace(sequenceStart, value);
                    }
                }
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowSequenceEnd(SequenceToken value)
        {
            if (EvaluateCurrent() is SequenceEnd sequenceEnd)
            {
                if(!string.IsNullOrEmpty(m_rawInput) && value != null)
                {
                    if(value.PreWhiteSpace != null) {
                        FillPostWhitespace(sequenceEnd, value);
                    } else {
                        value.PostWhiteSpace = new Position() { Line = sequenceEnd.End.Line, Character = sequenceEnd.End.Column };
                    }
                }
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
                if(!string.IsNullOrEmpty(m_rawInput))
                {
                    if(mappingStart.Style == MappingStyle.Block) {
                        FillPreWhitespace(mappingStart, value);
                    }
                }
                MoveNext();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean AllowMappingEnd(MappingToken value)
        {
            if (EvaluateCurrent() is MappingEnd mappingEnd)
            {
                if(!string.IsNullOrEmpty(m_rawInput) && value != null)
                {
                    if(value.PreWhiteSpace != null) {
                        FillPostWhitespace(mappingEnd, value);
                    } else {
                        value.PostWhiteSpace = new Position() { Line = mappingEnd.End.Line, Character = mappingEnd.End.Column };
                    }
                }
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

        private ParsingEvent EvaluateCurrent()
        {
            if (m_current == null)
            {
                m_current = m_parser.Current;
                if (m_current != null)
                {
                    if (m_current is Scalar scalar)
                    {
                        // Verify not using achors
                        if (scalar.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{scalar.Anchor}'");
                        }
                    }
                    else if (m_current is MappingStart mappingStart)
                    {
                        // Verify not using achors
                        if (mappingStart.Anchor != null)
                        {
                            throw new InvalidOperationException($"Anchors are not currently supported. Remove the anchor '{mappingStart.Anchor}'");
                        }
                    }
                    else if (m_current is SequenceStart sequenceStart)
                    {
                        // Verify not using achors
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

        private Boolean MoveNext()
        {
            m_current = null;
            return m_parser.MoveNext();
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
            if(m_force_azure_pipelines) {
                switch (scalar.Value ?? String.Empty)
                {
                    case "true":
                        value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, true, scalar.Value);
                        return true;
                    case "false":
                        value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, false, scalar.Value);
                        return true;
                }
            } else {
                // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
                switch (scalar.Value ?? String.Empty)
                {
                    case "true":
                    case "True":
                    case "TRUE":
                        value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, true, scalar.Value);
                        return true;
                    case "false":
                    case "False":
                    case "FALSE":
                        value = new BooleanToken(m_fileId, scalar.Start.Line, scalar.Start.Column, false, scalar.Value);
                        return true;
                }
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
                if(!m_force_azure_pipelines) {
                    // Check for [-+]?(\.inf|\.Inf|\.INF)|\.nan|\.NaN|\.NAN
                    switch (str)
                    {
                        case ".inf":
                        case ".Inf":
                        case ".INF":
                        case "+.inf":
                        case "+.Inf":
                        case "+.INF":
                            value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.PositiveInfinity, m_preserve_string ? str : null);
                            return true;
                        case "-.inf":
                        case "-.Inf":
                        case "-.INF":
                            value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.NegativeInfinity, m_preserve_string ? str : null);
                            return true;
                        case ".nan":
                        case ".NaN":
                        case ".NAN":
                            value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, Double.NaN, m_preserve_string ? str : null);
                            return true;
                    }
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
                            value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue, m_preserve_string ? str : null);
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
                                value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, (Double)doubleValue, m_preserve_string ? str : null);
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
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue, m_preserve_string ? str : null);
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
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, doubleValue, m_preserve_string ? str : null);
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
                        value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, integerValue, m_preserve_string ? str : null);
                        return true;
                    }

                    // Otherwise exceeds range
                    ThrowInvalidValue(scalar, c_integerTag); // throws
                }
                // Check for 0o[0-9]+
                else if (!m_force_azure_pipelines && firstChar == '0' &&
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

                    value = new NumberToken(m_fileId, scalar.Start.Line, scalar.Start.Column, integerValue, m_preserve_string ? str : null);
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
            if(m_force_azure_pipelines) {
                switch (scalar.Value ?? String.Empty)
                {
                    case "null":
                        value = new NullToken(m_fileId, scalar.Start.Line, scalar.Start.Column, m_preserve_string ? scalar.Value : null);
                        return true;
                }
            } else {
                // YAML 1.2 "core" schema https://yaml.org/spec/1.2/spec.html#id2804923
                switch (scalar.Value ?? String.Empty)
                {
                    case "":
                    case "null":
                    case "Null":
                    case "NULL":
                    case "~":
                        value = new NullToken(m_fileId, scalar.Start.Line, scalar.Start.Column, m_preserve_string ? scalar.Value : null);
                        return true;
                }
            }

            value = default;
            return false;
        }

        private void ThrowInvalidValue(
            Scalar scalar,
            String tag)
        {
            throw new NotSupportedException($"The value '{scalar.Value}' on line {scalar.Start.Line} and column {scalar.Start.Column} is invalid for the type '{scalar.Tag}'");
        }

        private const String c_booleanTag = "tag:yaml.org,2002:bool";
        private const String c_floatTag = "tag:yaml.org,2002:float";
        private const String c_integerTag = "tag:yaml.org,2002:int";
        private const String c_nullTag = "tag:yaml.org,2002:null";
        private const String c_stringTag = "tag:yaml.org,2002:string";
        private readonly Int32? m_fileId;
        private readonly IParser m_parser;
        private readonly bool m_yamlFold;
        private readonly bool m_preserve_string;
        private readonly bool m_force_azure_pipelines;
        private ParsingEvent m_current;
        private string m_rawInput;

    }
}
