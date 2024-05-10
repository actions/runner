using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using Index = GitHub.DistributedTask.Expressions2.Sdk.Operators.Index;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    public static class TemplateTokenExtensions
    {
        internal static string GetAssertPrefix(TemplateToken value) {
            var builder = new List<string>();
            if(value?.FileId != null) {
                builder.Add($"FileId: {value.FileId}");
            }
            if(value?.Line != null && value?.Column != null) {
                builder.Add(TemplateStrings.LineColumn(value?.Line, value?.Column) + ":");
            }
            return String.Join(" ", builder) + " ";
        }

        internal static BooleanToken AssertBoolean(
            this TemplateToken value,
            string objectDescription)
        {
            if (value is BooleanToken booleanToken)
            {
                return booleanToken;
            }

            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(BooleanToken)}' was expected.");
        }

        internal static NullToken AssertNull(
            this TemplateToken value,
            string objectDescription)
        {
            if (value is NullToken nullToken)
            {
                return nullToken;
            }

            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(NullToken)}' was expected.");
        }

        internal static NumberToken AssertNumber(
            this TemplateToken value,
            string objectDescription)
        {
            if (value is NumberToken numberToken)
            {
                return numberToken;
            }

            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(NumberToken)}' was expected.");
        }

        internal static StringToken AssertString(
            this TemplateToken value,
            string objectDescription)
        {
            if (value is StringToken stringToken)
            {
                return stringToken;
            }

            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(StringToken)}' was expected.");
        }

        internal static MappingToken AssertMapping(
            this TemplateToken value,
            string objectDescription)
        {
            if (value is MappingToken mapping)
            {
                return mapping;
            }

            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(MappingToken)}' was expected.");
        }

        internal static void AssertNotEmpty(
            this MappingToken mapping,
            string objectDescription)
        {
            if (mapping.Count == 0)
            {
                throw new ArgumentException($"{GetAssertPrefix(mapping)}Unexpected empty mapping when reading '{objectDescription}'");
            }
        }

        internal static ScalarToken AssertScalar(
            this TemplateToken value,
            string objectDescription)
        {
            if (value is ScalarToken scalar)
            {
                return scalar;
            }

            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(ScalarToken)}' was expected.");
        }

        internal static SequenceToken AssertSequence(
            this TemplateToken value,
            string objectDescription)
        {
            if (value is SequenceToken sequence)
            {
                return sequence;
            }

            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(SequenceToken)}' was expected.");
        }

        public static SequenceToken AssertScalarOrSequence(this TemplateToken token, string objectDescription) {
            switch(token.Type) {
                case TokenType.Boolean:
                case TokenType.Number:
                case TokenType.String:
                    var seq = new SequenceToken(null, null, null);
                    seq.Add(token);
                    return seq;
                default:
                    return token.AssertSequence(objectDescription);
            }
        }

        public static string AssertLiteralString(this TemplateToken value, string objectDescription) {
            if(value is LiteralToken literalToken) {
                return literalToken.ToString();
            }
            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(LiteralToken)}' was expected.");
        }

        public static bool TryParseAzurePipelinesBoolean(string literalString, out bool val) {
            if(string.Equals(literalString, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(literalString, "y", StringComparison.OrdinalIgnoreCase) || string.Equals(literalString, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(literalString, "on", StringComparison.OrdinalIgnoreCase)) {
                val = true;
                return true;
            }
            if(string.Equals(literalString, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(literalString, "n", StringComparison.OrdinalIgnoreCase) || string.Equals(literalString, "no", StringComparison.OrdinalIgnoreCase) || string.Equals(literalString, "off", StringComparison.OrdinalIgnoreCase)) {
                val = false;
                return true;
            }
            val = false;
            return false;
        }

        public static bool AssertAzurePipelinesBoolean(this TemplateToken value, string objectDescription) {
            string unexpectedValue = $"type '{value?.GetType().Name}'";
            if(value is LiteralToken literalToken) {
                var literalString = literalToken.ToString();
                if(TryParseAzurePipelinesBoolean(literalString, out var ret)) {
                    return ret;
                }
                unexpectedValue = $"value '{unexpectedValue}'";
            }
            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected {unexpectedValue} encountered while reading '{objectDescription}'. The type '{nameof(LiteralToken)}' with value true | y | yes | on | false | n | no | off was expected.");
        }

        public static int AssertAzurePipelinesInt32(this TemplateToken value, string objectDescription) {
            if(value is LiteralToken literalToken) {
                var literalString = literalToken.ToString();
                if(Int32.TryParse(literalString, out var ret)) {
                    return ret;
                }
                throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(LiteralToken)}' with an integer was expected, but got {literalString}.");
            }
            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(LiteralToken)}' with an integer was expected.");
        }

        public static double AssertAzurePipelinesDouble(this TemplateToken value, string objectDescription) {
            if(value is LiteralToken literalToken) {
                var literalString = literalToken.ToString();
                if(Double.TryParse(literalString, out var ret)) {
                    return ret;
                }
                throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(LiteralToken)}' with an integer was expected, but got {literalString}.");
            }
            throw new ArgumentException($"{GetAssertPrefix(value)}Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(LiteralToken)}' with double was expected.");
        }

        internal static void AssertUnexpectedValue(
            this LiteralToken literal,
            string objectDescription)
        {
            throw new ArgumentException($"{GetAssertPrefix(literal)}Error while reading '{objectDescription}'. Unexpected value '{literal.ToString()}'");
        }

        /// <summary>
        /// Traverses the token and checks whether a context pattern has been referenced inside an expression
        /// </summary>
        public static Boolean[] CheckReferencesContext(
            this TemplateToken token,
            String[] patterns,
            ExpressionFlags flags = ExpressionFlags.None)
        {
            var ret = new Boolean[patterns.Length];
            var expressionTokens = token.Traverse()
                .OfType<BasicExpressionToken>()
                .ToArray();
            var parser = new ExpressionParser() { Flags = flags };
            foreach (var expressionToken in expressionTokens)
            {
                var tree = parser.ValidateSyntax(expressionToken.Expression, null);
                var tmp = tree.CheckReferencesContext(patterns);
                for(int i = 0; i < tmp.Length; i++) {
                    ret[i] |= tmp[i];
                }
            }

            return ret;
        }

        /// <summary>
        /// Traverses the token and checks whether all required expression values
        /// and functions are provided.
        /// </summary>
        public static bool CheckHasRequiredContext(
            this TemplateToken token,
            IReadOnlyObject expressionValues,
            IList<IFunctionInfo> expressionFunctions,
            ExpressionFlags flags = ExpressionFlags.None)
        {
            var expressionTokens = token.Traverse()
                .OfType<BasicExpressionToken>()
                .ToArray();
            var parser = new ExpressionParser() { Flags = flags };
            foreach (var expressionToken in expressionTokens)
            {
                var tree = parser.ValidateSyntax(expressionToken.Expression, null);
                foreach (var node in tree.Traverse())
                {
                    if (node is NamedValue namedValue)
                    {
                        if (expressionValues?.Keys.Any(x => string.Equals(x, namedValue.Name, StringComparison.OrdinalIgnoreCase)) != true)
                        {
                            return false;
                        }
                    }
                    else if (node is Function function &&
                        !ExpressionConstants.WellKnownFunctions.ContainsKey(function.Name) &&
                        !((flags & ExpressionFlags.ExtendedFunctions) == ExpressionFlags.ExtendedFunctions && ExpressionConstants.AzureWellKnownFunctions.ContainsKey(function.Name)) &&
                        expressionFunctions?.Any(x => string.Equals(x.Name, function.Name, StringComparison.OrdinalIgnoreCase)) != true)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static IEnumerable<string> CheckUnknownParameters(
            this TemplateToken token,
            string context,
            string[] names,
            ExpressionFlags flags = ExpressionFlags.None)
        {
            var expressionTokens = token.Traverse()
                .OfType<BasicExpressionToken>()
                .ToArray();
            var parser = new ExpressionParser() { Flags = flags };
            foreach (var expressionToken in expressionTokens)
            {
                var tree = parser.ValidateSyntax(expressionToken.Expression, null);
                foreach (var node in tree.Traverse())
                {
                    if (node is Index indexValue && indexValue.Parameters[0] is NamedValue namedValue && string.Equals(namedValue.Name, context, StringComparison.OrdinalIgnoreCase) && indexValue.Parameters[1] is Literal literal && literal.Value is String literalString && names.All(name => !string.Equals(literalString, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        yield return literalString;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all tokens (depth first)
        /// </summary>
        public static IEnumerable<TemplateToken> Traverse(this TemplateToken token)
        {
            return Traverse(token, omitKeys: false);
        }

        /// <summary>
        /// Returns all tokens (depth first)
        /// </summary>
        public static IEnumerable<TemplateToken> Traverse(
            this TemplateToken token,
            bool omitKeys)
        {
            if (token != null)
            {
                yield return token;

                if (token is SequenceToken || token is MappingToken)
                {
                    var state = new TraversalState(null, token);
                    while (state != null)
                    {
                        if (state.MoveNext(omitKeys))
                        {
                            token = state.Current;
                            yield return token;

                            if (token is SequenceToken || token is MappingToken)
                            {
                                state = new TraversalState(state, token);
                            }
                        }
                        else
                        {
                            state = state.Parent;
                        }
                    }
                }
            }
        }

        private sealed class TraversalState
        {
            public TraversalState(
                TraversalState parent,
                TemplateToken token)
            {
                Parent = parent;
                m_token = token;
            }

            public bool MoveNext(bool omitKeys)
            {
                switch (m_token.Type)
                {
                    case TokenType.Sequence:
                        var sequence = m_token as SequenceToken;
                        if (++m_index < sequence.Count)
                        {
                            Current = sequence[m_index];
                            return true;
                        }
                        else
                        {
                            Current = null;
                            return false;
                        }

                    case TokenType.Mapping:
                        var mapping = m_token as MappingToken;

                        // Return the value
                        if (m_isKey)
                        {
                            m_isKey = false;
                            Current = mapping[m_index].Value;
                            return true;
                        }

                        if (++m_index < mapping.Count)
                        {
                            // Skip the key, return the value
                            if (omitKeys)
                            {
                                m_isKey = false;
                                Current = mapping[m_index].Value;
                                return true;
                            }

                            // Return the key
                            m_isKey = true;
                            Current = mapping[m_index].Key;
                            return true;
                        }

                        Current = null;
                        return false;

                    default:
                        throw new NotSupportedException($"Unexpected token type '{m_token.Type}'");
                }
            }

            private TemplateToken m_token;
            private int m_index = -1;
            private bool m_isKey;
            public TemplateToken Current;
            public TraversalState Parent;
        }
    }
}
