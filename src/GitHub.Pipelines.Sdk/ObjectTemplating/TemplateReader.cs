using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Schema;
using System.Collections;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating
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
            if (m_objectReader.AllowScalar(out Int32? line, out Int32? column, out String rawScalar))
            {
                var scalar = ParseScalar(line, column, rawScalar, definition.AllowedContext);
                m_memory.AddBytes(scalar);
                Validate(scalar, definition);
                return scalar;
            }

            // Sequence
            if (m_objectReader.AllowSequenceStart(out line, out column))
            {
                m_memory.IncrementDepth();
                var sequence = new SequenceToken(m_fileId, line, column);
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
            if (m_objectReader.AllowMappingStart(out line, out column))
            {
                m_memory.IncrementDepth();
                var mapping = new MappingToken(m_fileId, line, column);
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

            while (m_objectReader.AllowScalar(out Int32? line, out Int32? column, out String rawScalar))
            {
// todo: switch to key-definition?
                var nextKeyScalar = ParseScalar(line, column, rawScalar, definition.AllowedContext);

                // Expression
                if (!(nextKeyScalar is LiteralToken nextKey))
                {
                    // Legal
                    if (definition.AllowedContext.Length > 0)
                    {
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

            ExpectMappingEnd();
        }

        private void HandleMappingWithAllLooseProperties(
            DefinitionInfo mappingDefinition,
            DefinitionInfo keyDefinition,
            DefinitionInfo valueDefinition,
            MappingToken mapping)
        {
            var keys = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

            while (m_objectReader.AllowScalar(out Int32? line, out Int32? column, out String rawScalar))
            {
                var nextKeyScalar = ParseScalar(line, column, rawScalar, mappingDefinition.AllowedContext);

                // Expression
                if (!(nextKeyScalar is LiteralToken nextKey))
                {
                    // Legal
                    if (mappingDefinition.AllowedContext.Length > 0)
                    {
                        var nextValue = ReadValue(valueDefinition);
                        mapping.Add(nextKeyScalar, nextValue);
                    }
                    // Illegal
                    else
                    {
                        m_context.Error(nextKeyScalar, TemplateStrings.ExpressionNotAllowed());
                        SkipValue();
                    }
                }
                // Literal
                else
                {
                    // Duplicate
                    if (!keys.Add(nextKey.Value))
                    {
                        m_context.Error(nextKey, TemplateStrings.ValueAlreadyDefined(nextKey.Value));
                        SkipValue();
                    }
                    // Not duplicate
                    else
                    {
                        Validate(nextKey, keyDefinition);
                        var nextValue = ReadValue(valueDefinition);
                        mapping.Add(nextKey, nextValue);
                    }
                }
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
            Int32? line;
            Int32? column;

            // Scalar
            if (m_objectReader.AllowScalar(out line, out column, out String rawValue))
            {
                if (error)
                {
                    m_context.Error(m_fileId, line, column, TemplateStrings.UnexpectedValue(rawValue));
                }

                return;
            }

            // Sequence
            if (m_objectReader.AllowSequenceStart(out line, out column))
            {
                m_memory.IncrementDepth();

                if (error)
                {
                    m_context.Error(m_fileId, line, column, TemplateStrings.UnexpectedSequenceStart());
                }

                while (!m_objectReader.AllowSequenceEnd())
                {
                    SkipValue();
                }

                m_memory.DecrementDepth();
                return;
            }

            // Mapping
            if (m_objectReader.AllowMappingStart(out line, out column))
            {
                m_memory.IncrementDepth();

                if (error)
                {
                    m_context.Error(m_fileId, line, column, TemplateStrings.UnexpectedMappingStart());
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
            ScalarToken scalar,
            DefinitionInfo definition)
        {
            switch (scalar.Type)
            {
                case TokenType.Literal:
                    var literal = scalar as LiteralToken;

                    // Illegal value
                    if (!definition.Get<ScalarDefinition>().Any(x => x.IsMatch(literal)))
                    {
                        m_context.Error(literal, TemplateStrings.UnexpectedValue(literal.Value));
                    }

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
            Int32? line,
            Int32? column,
            String raw,
            String[] allowedContext)
        {
            // Check if the value is definitely a literal
            Int32 startExpression;
            if (String.IsNullOrEmpty(raw) ||
                (startExpression = raw.IndexOf(c_openExpression)) < 0) // Doesn't contain ${{
            {
                return new LiteralToken(m_fileId, line, column, raw);
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
                    for (i += c_openExpression.Length; i < raw.Length; i++)
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
                        m_context.Error(m_fileId, line, column, TemplateStrings.ExpressionNotClosed());
                        return new LiteralToken(m_fileId, line, column, raw);
                    }

                    // Parse the expression
                    var rawExpression = raw.Substring(
                        startExpression + c_openExpression.Length,
                        endExpression - startExpression + 1 - c_openExpression.Length - c_closeExpression.Length);
                    var expression = ParseExpression(line, column, rawExpression, allowedContext, out Exception ex);

                    // Check for error
                    if (ex != null)
                    {
                        m_context.Error(m_fileId, line, column, ex);
                        return new LiteralToken(m_fileId, line, column, raw);
                    }

                    // Check if a directive was used when not allowed
                    if (!String.IsNullOrEmpty(expression.Directive) &&
                        ((startExpression != 0) || (i < raw.Length)))
                    {
                        m_context.Error(m_fileId, line, column, TemplateStrings.DirectiveNotAllowedInline(expression.Directive));
                        return new LiteralToken(m_fileId, line, column, raw);
                    }

                    // Add the segment
                    segments.Add(expression);

                    // Look for the next expression
                    startExpression = raw.IndexOf(c_openExpression, i);
                }
                // The next expression is further ahead:
                else if (i < startExpression)
                {
                    // Append the segment
                    AddLiteral(segments, line, column, raw.Substring(i, startExpression - i));

                    // Adjust the position
                    i = startExpression;
                }
                // No remaining expressions:
                else
                {
                    AddLiteral(segments, line, column, raw.Substring(i));
                    break;
                }
            }

            // Check if can convert to a literal
            // For example, the escaped expression: ${{ '{{ this is a literal }}' }}
            if (segments.Count == 1 &&
                segments[0] is BasicExpressionToken basicExpression &&
                IsExpressionString(basicExpression.Expression, out String str))
            {
                return new LiteralToken(m_fileId, line, column, str);
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
                if (segment is LiteralToken literal)
                {
                    var text = ExpressionUtil.StringEscape(literal.Value) // Escape quotes
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
// todo: does this null ref for the inline expression "asdf ${{ insert }}"
                    args.Append(expression.Expression);
                }
            }

            return new BasicExpressionToken(m_fileId, line, column, $"format('{format}'{args})");
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

        private void AddLiteral(
            List<ScalarToken> segments,
            Int32? line,
            Int32? column,
            String value)
        {
            // If the last segment was a LiteralToken, then append to the last segment
            if (segments.Count > 0 && segments[segments.Count - 1] is LiteralToken lastSegment)
            {
                segments[segments.Count - 1] = new LiteralToken(m_fileId, line, column, lastSegment.Value + value);
            }
            // Otherwise add a new LiteralToken
            else
            {
                segments.Add(new LiteralToken(m_fileId, line, column, value));
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

                // Determine whether to expand
                if (Definition.Context.Length > 0)
                {
                    AllowedContext = Definition.Context;
                }
                else
                {
                    AllowedContext = new String[0];
                }
            }

            public DefinitionInfo(
                DefinitionInfo parent,
                String name)
            {
                m_schema = parent.m_schema;

                // Lookup the definition
                Definition = m_schema.GetDefinition(name);

                // Determine whether to expand
                if (Definition.Context.Length > 0)
                {
                    AllowedContext = new HashSet<String>(parent.AllowedContext.Concat(Definition.Context)).ToArray();
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

        private const String c_openExpression = "${{";
        private const String c_closeExpression = "}}";
        private readonly TemplateContext m_context;
        private readonly Int32? m_fileId;
        private readonly TemplateMemory m_memory;
        private readonly IObjectReader m_objectReader;
        private readonly TemplateSchema m_schema;
    }
}
