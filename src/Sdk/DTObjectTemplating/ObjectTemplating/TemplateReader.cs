using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.ObjectTemplating.Schema;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Converts a source object format into a TemplateToken
    /// </summary>
    internal sealed class TemplateReader
    {
        private TemplateReader(
            TemplateContext context,
            TemplateSchema schema,
            IObjectReader objectReader,
            Int32? fileId)
        {
            m_context = context;
            m_schema = schema;
            m_memory = context.Memory;
            m_objectReader = objectReader;
            m_fileId = fileId;
        }

        internal static TemplateToken Read(
            TemplateContext context,
            String type,
            IObjectReader objectReader,
            Int32? fileId,
            out Int32 bytes)
        {
            return Read(context, type, objectReader, fileId, context.Schema, out bytes);
        }

        internal static TemplateToken Read(
            TemplateContext context,
            String type,
            IObjectReader objectReader,
            Int32? fileId,
            TemplateSchema schema,
            out Int32 bytes)
        {
            TemplateToken result = null;

            var reader = new TemplateReader(context, schema, objectReader, fileId);
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                objectReader.ValidateStart();
                var definition = new DefinitionInfo(schema, type);
                result = reader.ReadValue(definition);
                objectReader.ValidateEnd();
            }
            catch (Exception ex)
            {
                context.Error(fileId, null, null, ex);
            }
            finally
            {
                bytes = context.Memory.CurrentBytes - originalBytes;
            }

            return result;
        }

        private TemplateToken ReadValue(DefinitionInfo definition)
        {
            m_memory.IncrementEvents();

            // Scalar
            if (m_objectReader.AllowLiteral(out LiteralToken literal))
            {
                var scalar = ParseScalar(literal, definition.AllowedContext);
                Validate(ref scalar, definition);
                m_memory.AddBytes(scalar);
                return scalar;
            }

            // Sequence
            if (m_objectReader.AllowSequenceStart(out SequenceToken sequence))
            {
                m_memory.IncrementDepth();
                m_memory.AddBytes(sequence);

                var sequenceDefinition = definition.Get<SequenceDefinition>().FirstOrDefault();

                // Legal
                if (sequenceDefinition != null)
                {
                    var itemDefinition = new DefinitionInfo(definition, sequenceDefinition.ItemType);

                    // Add each item
                    while (!m_objectReader.AllowSequenceEnd())
                    {
                        var item = ReadValue(itemDefinition);
                        sequence.Add(item);
                    }
                }
                // Illegal
                else
                {
                    // Error
                    m_context.Error(sequence, TemplateStrings.UnexpectedSequenceStart());

                    // Skip each item
                    while (!m_objectReader.AllowSequenceEnd())
                    {
                        SkipValue();
                    }
                }

                m_memory.DecrementDepth();
                return sequence;
            }

            // Mapping
            if (m_objectReader.AllowMappingStart(out MappingToken mapping))
            {
                m_memory.IncrementDepth();
                m_memory.AddBytes(mapping);

                var mappingDefinitions = definition.Get<MappingDefinition>().ToList();

                // Legal
                if (mappingDefinitions.Count > 0)
                {
                    if (mappingDefinitions.Count > 1 ||
                        m_schema.HasProperties(mappingDefinitions[0]) ||
                        String.IsNullOrEmpty(mappingDefinitions[0].LooseKeyType))
                    {
                        HandleMappingWithWellKnownProperties(definition, mappingDefinitions, mapping);
                    }
                    else
                    {
                        var keyDefinition = new DefinitionInfo(definition, mappingDefinitions[0].LooseKeyType);
                        var valueDefinition = new DefinitionInfo(definition, mappingDefinitions[0].LooseValueType);
                        HandleMappingWithAllLooseProperties(definition, keyDefinition, valueDefinition, mapping);
                    }
                }
                // Illegal
                else
                {
                    m_context.Error(mapping, TemplateStrings.UnexpectedMappingStart());

                    while (!m_objectReader.AllowMappingEnd())
                    {
                        SkipValue();
                        SkipValue();
                    }
                }

                m_memory.DecrementDepth();
                return mapping;
            }

            throw new InvalidOperationException(TemplateStrings.ExpectedScalarSequenceOrMapping());
        }

        private void HandleMappingWithWellKnownProperties(
            DefinitionInfo definition,
            List<MappingDefinition> mappingDefinitions,
            MappingToken mapping)
        {
            // Check if loose properties are allowed
            String looseKeyType = null;
            String looseValueType = null;
            DefinitionInfo? looseKeyDefinition = null;
            DefinitionInfo? looseValueDefinition = null;
            if (!String.IsNullOrEmpty(mappingDefinitions[0].LooseKeyType))
            {
                looseKeyType = mappingDefinitions[0].LooseKeyType;
                looseValueType = mappingDefinitions[0].LooseValueType;
            }

            var keys = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            var hasExpressionKey = false;

            while (m_objectReader.AllowLiteral(out LiteralToken rawLiteral))
            {
                var nextKeyScalar = ParseScalar(rawLiteral, definition.AllowedContext);
                // Expression
                if (nextKeyScalar is ExpressionToken)
                {
                    hasExpressionKey = true;
                    // Legal
                    if (definition.AllowedContext.Length > 0)
                    {
                        m_memory.AddBytes(nextKeyScalar);
                        var anyDefinition = new DefinitionInfo(definition, TemplateConstants.Any);
                        mapping.Add(nextKeyScalar, ReadValue(anyDefinition));
                    }
                    // Illegal
                    else
                    {
                        m_context.Error(nextKeyScalar, TemplateStrings.ExpressionNotAllowed());
                        SkipValue();
                    }

                    continue;
                }

                // Not a string, convert
                if (!(nextKeyScalar is StringToken nextKey))
                {
                    nextKey = new StringToken(nextKeyScalar.FileId, nextKeyScalar.Line, nextKeyScalar.Column, nextKeyScalar.ToString());
                }

                // Duplicate
                if (!keys.Add(nextKey.Value))
                {
                    m_context.Error(nextKey, TemplateStrings.ValueAlreadyDefined(nextKey.Value));
                    SkipValue();
                    continue;
                }

                // Well known
                if (m_schema.TryMatchKey(mappingDefinitions, nextKey.Value, out String nextValueType))
                {
                    m_memory.AddBytes(nextKey);
                    var nextValueDefinition = new DefinitionInfo(definition, nextValueType);
                    var nextValue = ReadValue(nextValueDefinition);
                    mapping.Add(nextKey, nextValue);
                    continue;
                }

                // Loose
                if (looseKeyType != null)
                {
                    if (looseKeyDefinition == null)
                    {
                        looseKeyDefinition = new DefinitionInfo(definition, looseKeyType);
                        looseValueDefinition = new DefinitionInfo(definition, looseValueType);
                    }

                    Validate(nextKey, looseKeyDefinition.Value);
                    m_memory.AddBytes(nextKey);
                    var nextValue = ReadValue(looseValueDefinition.Value);
                    mapping.Add(nextKey, nextValue);
                    continue;
                }

                // Error
                m_context.Error(nextKey, TemplateStrings.UnexpectedValue(nextKey.Value));
                SkipValue();
            }

            // Only one
            if (mappingDefinitions.Count > 1)
            {
                var hitCount = new Dictionary<String, Int32>();
                foreach (MappingDefinition mapdef in mappingDefinitions)
                {
                    foreach (String key in mapdef.Properties.Keys)
                    {
                        if (!hitCount.TryGetValue(key, out Int32 value))
                        {
                            hitCount.Add(key, 1);
                        }
                        else
                        {
                            hitCount[key] = value + 1;
                        }
                    }
                }

                List<String> nonDuplicates = new List<String>();
                foreach (String key in hitCount.Keys)
                {
                    if(hitCount[key] == 1)
                    {
                        nonDuplicates.Add(key);
                    }
                }
                nonDuplicates.Sort();

                String listToDeDuplicate = String.Join(", ", nonDuplicates);
                m_context.Error(mapping, TemplateStrings.UnableToDetermineOneOf(listToDeDuplicate));
            }
            else if (mappingDefinitions.Count == 1 && !hasExpressionKey)
            {
                foreach (var property in mappingDefinitions[0].Properties)
                {
                    if (property.Value.Required)
                    {
                        if (!keys.Contains(property.Key))
                        {
                            m_context.Error(mapping, $"Required property is missing: {property.Key}");
                        }
                    }
                }
            }
            ExpectMappingEnd();
        }

        private void HandleMappingWithAllLooseProperties(
            DefinitionInfo mappingDefinition,
            DefinitionInfo keyDefinition,
            DefinitionInfo valueDefinition,
            MappingToken mapping)
        {
            TemplateToken nextValue;
            var keys = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            while (m_objectReader.AllowLiteral(out LiteralToken rawLiteral))
            {
                var nextKeyScalar = ParseScalar(rawLiteral, mappingDefinition.AllowedContext);

                // Expression
                if (nextKeyScalar is ExpressionToken)
                {
                    // Legal
                    if (mappingDefinition.AllowedContext.Length > 0)
                    {
                        m_memory.AddBytes(nextKeyScalar);
                        nextValue = ReadValue(valueDefinition);
                        mapping.Add(nextKeyScalar, nextValue);
                    }
                    // Illegal
                    else
                    {
                        m_context.Error(nextKeyScalar, TemplateStrings.ExpressionNotAllowed());
                        SkipValue();
                    }

                    continue;
                }

                // Not a string, convert
                if (!(nextKeyScalar is StringToken nextKey))
                {
                    nextKey = new StringToken(nextKeyScalar.FileId, nextKeyScalar.Line, nextKeyScalar.Column, nextKeyScalar.ToString());
                }

                // Duplicate
                if (!keys.Add(nextKey.Value))
                {
                    m_context.Error(nextKey, TemplateStrings.ValueAlreadyDefined(nextKey.Value));
                    SkipValue();
                    continue;
                }

                // Validate
                Validate(nextKey, keyDefinition);
                m_memory.AddBytes(nextKey);

                // Add the pair
                nextValue = ReadValue(valueDefinition);
                mapping.Add(nextKey, nextValue);
            }

            ExpectMappingEnd();
        }

        private void ExpectMappingEnd()
        {
            if (!m_objectReader.AllowMappingEnd())
            {
                throw new Exception("Expected mapping end"); // Should never happen
            }
        }

        private void SkipValue(Boolean error = false)
        {
            m_memory.IncrementEvents();

            // Scalar
            if (m_objectReader.AllowLiteral(out LiteralToken literal))
            {
                if (error)
                {
                    m_context.Error(literal, TemplateStrings.UnexpectedValue(literal));
                }

                return;
            }

            // Sequence
            if (m_objectReader.AllowSequenceStart(out SequenceToken sequence))
            {
                m_memory.IncrementDepth();

                if (error)
                {
                    m_context.Error(sequence, TemplateStrings.UnexpectedSequenceStart());
                }

                while (!m_objectReader.AllowSequenceEnd())
                {
                    SkipValue();
                }

                m_memory.DecrementDepth();
                return;
            }

            // Mapping
            if (m_objectReader.AllowMappingStart(out MappingToken mapping))
            {
                m_memory.IncrementDepth();

                if (error)
                {
                    m_context.Error(mapping, TemplateStrings.UnexpectedMappingStart());
                }

                while (!m_objectReader.AllowMappingEnd())
                {
                    SkipValue();
                    SkipValue();
                }

                m_memory.DecrementDepth();
                return;
            }

            // Unexpected
            throw new InvalidOperationException(TemplateStrings.ExpectedScalarSequenceOrMapping());
        }

        private void Validate(
            StringToken stringToken,
            DefinitionInfo definition)
        {
            var scalar = stringToken as ScalarToken;
            Validate(ref scalar, definition);
        }

        private void Validate(
            ref ScalarToken scalar,
            DefinitionInfo definition)
        {
            switch (scalar.Type)
            {
                case TokenType.Null:
                case TokenType.Boolean:
                case TokenType.Number:
                case TokenType.String:
                    var literal = scalar as LiteralToken;

                    // Legal
                    if (definition.Get<ScalarDefinition>().Any(x => x.IsMatch(literal)))
                    {
                        return;
                    }

                    // Not a string, convert
                    if (literal.Type != TokenType.String)
                    {
                        literal = new StringToken(literal.FileId, literal.Line, literal.Column, literal.ToString());

                        // Legal
                        if (definition.Get<StringDefinition>().Any(x => x.IsMatch(literal)))
                        {
                            scalar = literal;
                            return;
                        }
                    }

                    // Illegal
                    m_context.Error(literal, TemplateStrings.UnexpectedValue(literal));
                    break;

                case TokenType.BasicExpression:

                    // Illegal
                    if (definition.AllowedContext.Length == 0)
                    {
                        m_context.Error(scalar, TemplateStrings.ExpressionNotAllowed());
                    }

                    break;

                default:
                    m_context.Error(scalar, TemplateStrings.UnexpectedValue(scalar));
                    break;
            }
        }

        private ScalarToken ParseScalar(
            LiteralToken token,
            String[] allowedContext)
        {
            // Not a string
            if (token.Type != TokenType.String)
            {
                return token;
            }

            // Check if the value is definitely a literal
            var raw = token.ToString();
            Int32 startExpression;
            if (String.IsNullOrEmpty(raw) ||
                (startExpression = raw.IndexOf(TemplateConstants.OpenExpression)) < 0) // Doesn't contain ${{
            {
                return token;
            }

            // Break the value into segments of LiteralToken and ExpressionToken
            var segments = new List<ScalarToken>();
            var i = 0;
            while (i < raw.Length)
            {
                // An expression starts here:
                if (i == startExpression)
                {
                    // Find the end of the expression - i.e. }}
                    startExpression = i;
                    var endExpression = -1;
                    var inString = false;
                    for (i += TemplateConstants.OpenExpression.Length; i < raw.Length; i++)
                    {
                        if (raw[i] == '\'')
                        {
                            inString = !inString; // Note, this handles escaped single quotes gracefully. Ex. 'foo''bar'
                        }
                        else if (!inString && raw[i] == '}' && raw[i - 1] == '}')
                        {
                            endExpression = i;
                            i++;
                            break;
                        }
                    }

                    // Check if not closed
                    if (endExpression < startExpression)
                    {
                        m_context.Error(token, TemplateStrings.ExpressionNotClosed());
                        return token;
                    }

                    // Parse the expression
                    var rawExpression = raw.Substring(
                        startExpression + TemplateConstants.OpenExpression.Length,
                        endExpression - startExpression + 1 - TemplateConstants.OpenExpression.Length - TemplateConstants.CloseExpression.Length);
                    var expression = ParseExpression(token.Line, token.Column, rawExpression, allowedContext, out Exception ex);

                    // Check for error
                    if (ex != null)
                    {
                        m_context.Error(token, ex);
                        return token;
                    }

                    // Check if a directive was used when not allowed
                    if (!String.IsNullOrEmpty(expression.Directive) &&
                        ((startExpression != 0) || (i < raw.Length)))
                    {
                        m_context.Error(token, TemplateStrings.DirectiveNotAllowedInline(expression.Directive));
                        return token;
                    }

                    // Add the segment
                    segments.Add(expression);

                    // Look for the next expression
                    startExpression = raw.IndexOf(TemplateConstants.OpenExpression, i);
                }
                // The next expression is further ahead:
                else if (i < startExpression)
                {
                    // Append the segment
                    AddString(segments, token.Line, token.Column, raw.Substring(i, startExpression - i));

                    // Adjust the position
                    i = startExpression;
                }
                // No remaining expressions:
                else
                {
                    AddString(segments, token.Line, token.Column, raw.Substring(i));
                    break;
                }
            }

            // Check if can convert to a literal
            // For example, the escaped expression: ${{ '{{ this is a literal }}' }}
            if (segments.Count == 1 &&
                segments[0] is BasicExpressionToken basicExpression &&
                IsExpressionString(basicExpression.Expression, out String str))
            {
                return new StringToken(m_fileId, token.Line, token.Column, str);
            }

            // Check if only ony segment
            if (segments.Count == 1)
            {
                return segments[0];
            }

            // Build the new expression, using the format function
            var format = new StringBuilder();
            var args = new StringBuilder();
            var argIndex = 0;
            foreach (var segment in segments)
            {
                if (segment is StringToken literal)
                {
                    var text = ExpressionUtility.StringEscape(literal.Value) // Escape quotes
                        .Replace("{", "{{") // Escape braces
                        .Replace("}", "}}");
                    format.Append(text);
                }
                else
                {
                    format.Append("{" + argIndex.ToString(CultureInfo.InvariantCulture) + "}"); // Append formatter
                    argIndex++;

                    var expression = segment as BasicExpressionToken;
                    args.Append(", ");
                    args.Append(expression.Expression);
                }
            }

            return new BasicExpressionToken(m_fileId, token.Line, token.Column, $"format('{format}'{args})");
        }

        private ExpressionToken ParseExpression(
            Int32? line,
            Int32? column,
            String value,
            String[] allowedContext,
            out Exception ex)
        {
            var trimmed = value.Trim();

            // Check if the value is empty
            if (String.IsNullOrEmpty(trimmed))
            {
                ex = new ArgumentException(TemplateStrings.ExpectedExpression());
                return null;
            }

            // Try to find a matching directive
            List<String> parameters;
            if (MatchesDirective(trimmed, TemplateConstants.InsertDirective, 0, out parameters, out ex))
            {
                return new InsertExpressionToken(m_fileId, line, column);
            }
            else if (ex != null)
            {
                return null;
            }

            // Check if the value is an expression
            if (!ExpressionToken.IsValidExpression(trimmed, allowedContext, out ex))
            {
                return null;
            }

            // Return the expression
            return new BasicExpressionToken(m_fileId, line, column, trimmed);
        }

        private void AddString(
            List<ScalarToken> segments,
            Int32? line,
            Int32? column,
            String value)
        {
            // If the last segment was a LiteralToken, then append to the last segment
            if (segments.Count > 0 && segments[segments.Count - 1] is StringToken lastSegment)
            {
                segments[segments.Count - 1] = new StringToken(m_fileId, line, column, lastSegment.Value + value);
            }
            // Otherwise add a new LiteralToken
            else
            {
                segments.Add(new StringToken(m_fileId, line, column, value));
            }
        }

        private static Boolean MatchesDirective(
            String trimmed,
            String directive,
            Int32 expectedParameters,
            out List<String> parameters,
            out Exception ex)
        {
            if (trimmed.StartsWith(directive, StringComparison.Ordinal) &&
                (trimmed.Length == directive.Length || Char.IsWhiteSpace(trimmed[directive.Length])))
            {
                parameters = new List<String>();
                var startIndex = directive.Length;
                var inString = false;
                var parens = 0;
                for (var i = startIndex; i < trimmed.Length; i++)
                {
                    var c = trimmed[i];
                    if (Char.IsWhiteSpace(c) && !inString && parens == 0)
                    {
                        if (startIndex < i)
                        {
                            parameters.Add(trimmed.Substring(startIndex, i - startIndex));
                        }

                        startIndex = i + 1;
                    }
                    else if (c == '\'')
                    {
                        inString = !inString;
                    }
                    else if (c == '(' && !inString)
                    {
                        parens++;
                    }
                    else if (c == ')' && !inString)
                    {
                        parens--;
                    }
                }

                if (startIndex < trimmed.Length)
                {
                    parameters.Add(trimmed.Substring(startIndex));
                }

                if (expectedParameters != parameters.Count)
                {
                    ex = new ArgumentException(TemplateStrings.ExpectedNParametersFollowingDirective(expectedParameters, directive, parameters.Count));
                    parameters = null;
                    return false;
                }

                ex = null;
                return true;
            }

            ex = null;
            parameters = null;
            return false;
        }

        private static Boolean IsExpressionString(
            String trimmed,
            out String str)
        {
            var builder = new StringBuilder();

            var inString = false;
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];
                if (c == '\'')
                {
                    inString = !inString;

                    if (inString && i != 0)
                    {
                        builder.Append(c);
                    }
                }
                else if (!inString)
                {
                    str = default;
                    return false;
                }
                else
                {
                    builder.Append(c);
                }
            }

            str = builder.ToString();
            return true;
        }

        private struct DefinitionInfo
        {
            public DefinitionInfo(
                TemplateSchema schema,
                String name)
            {
                m_schema = schema;

                // Lookup the definition
                Definition = m_schema.GetDefinition(name);

                // Record allowed context
                AllowedContext = Definition.ReaderContext;
            }

            public DefinitionInfo(
                DefinitionInfo parent,
                String name)
            {
                m_schema = parent.m_schema;

                // Lookup the definition
                Definition = m_schema.GetDefinition(name);

                // Record allowed context
                if (Definition.ReaderContext.Length > 0)
                {
                    AllowedContext = new HashSet<String>(parent.AllowedContext.Concat(Definition.ReaderContext), StringComparer.OrdinalIgnoreCase).ToArray();
                }
                else
                {
                    AllowedContext = parent.AllowedContext;
                }
            }

            public IEnumerable<T> Get<T>()
                where T : Definition
            {
                return m_schema.Get<T>(Definition);
            }

            private TemplateSchema m_schema;
            public Definition Definition;
            public String[] AllowedContext;
        }

        private readonly TemplateContext m_context;
        private readonly Int32? m_fileId;
        private readonly TemplateMemory m_memory;
        private readonly IObjectReader m_objectReader;
        private readonly TemplateSchema m_schema;
    }
}
