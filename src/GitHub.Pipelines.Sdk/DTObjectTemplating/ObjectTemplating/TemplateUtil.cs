using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating
{
    internal static class TemplateUtil
    {
        internal static LiteralToken AssertLiteral(
            TemplateToken value,
            String objectDescription)
        {
            if (value is LiteralToken literal)
            {
                return literal;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(LiteralToken)}' was expected.");
        }

        internal static MappingToken AssertMapping(
            TemplateToken value,
            String objectDescription)
        {
            if (value is MappingToken mapping)
            {
                return mapping;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(MappingToken)}' was expected.");
        }

        internal static void AssertNotEmpty(
            MappingToken mapping,
            String objectDescription)
        {
            if (mapping.Count == 0)
            {
                throw new ArgumentException($"Unexpected empty mapping when reading '{objectDescription}'");
            }
        }

        internal static ScalarToken AssertScalar(
            TemplateToken value,
            String objectDescription)
        {
            if (value is ScalarToken scalar)
            {
                return scalar;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(ScalarToken)}' was expected.");
        }

        internal static SequenceToken AssertSequence(
            TemplateToken value,
            String objectDescription)
        {
            if (value is SequenceToken sequence)
            {
                return sequence;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(SequenceToken)}' was expected.");
        }

        internal static void AssertUnexpectedValue(
            LiteralToken literal,
            String objectDescription)
        {
            throw new ArgumentException($"Error while reading '{objectDescription}'. Unexpected value '{literal.Value}'");
        }

        /// <summary>
        /// Returns all tokens (depth first)
        /// </summary>
        internal static IEnumerable<TemplateToken> GetTokens(TemplateToken token)
        {
            return GetTokens(token, omitKeys: false);
        }

        /// <summary>
        /// Returns all tokens (depth first)
        /// </summary>
        internal static IEnumerable<TemplateToken> GetTokens(
            TemplateToken token,
            Boolean omitKeys)
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

        private sealed class TraversalState
        {
            public TraversalState(
                TraversalState parent,
                TemplateToken token)
            {
                Parent = parent;
                m_token = token;
            }

            public Boolean MoveNext(Boolean omitKeys)
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
            private Int32 m_index = -1;
            private Boolean m_isKey;
            public TemplateToken Current;
            public TraversalState Parent;
        }
    }
}
