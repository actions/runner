using GitHub.DistributedTask.Expressions2.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GitHub.DistributedTask.Expressions2.Tokens
{
    internal sealed class LexicalAnalyzer
    {
        public LexicalAnalyzer(String expression)
        {
            m_expression = expression;
        }

        public IEnumerable<Token> UnclosedTokens => m_unclosedTokens;

        public Boolean TryGetNextToken(ref Token token)
        {
            // Skip whitespace
            while (m_index < m_expression.Length && Char.IsWhiteSpace(m_expression[m_index]))
            {
                m_index++;
            }

            // Test end of string
            if (m_index >= m_expression.Length)
            {
                token = null;
                return false;
            }

            // Read the first character to determine the type of token.
            var c = m_expression[m_index];
            switch (c)
            {
                case ExpressionConstants.StartGroup:    // "("
                    // Function call
                    if (m_lastToken?.Kind == TokenKind.Function)
                    {
                        token = CreateToken(TokenKind.StartParameters, c, m_index++);
                    }
                    // Logical grouping
                    else
                    {
                        token = CreateToken(TokenKind.StartGroup, c, m_index++);
                    }
                    break;
                case ExpressionConstants.StartIndex:    // "["
                    token = CreateToken(TokenKind.StartIndex, c, m_index++);
                    break;
                case ExpressionConstants.EndGroup:      // ")"
                    // Function call
                    if (m_unclosedTokens.FirstOrDefault()?.Kind == TokenKind.StartParameters) // "(" function call
                    {
                        token = CreateToken(TokenKind.EndParameters, c, m_index++);
                    }
                    // Logical grouping
                    else
                    {
                        token = CreateToken(TokenKind.EndGroup, c, m_index++);
                    }
                    break;
                case ExpressionConstants.EndIndex:      // "]"
                    token = CreateToken(TokenKind.EndIndex, c, m_index++);
                    break;
                case ExpressionConstants.Separator:     // ","
                    token = CreateToken(TokenKind.Separator, c, m_index++);
                    break;
                case ExpressionConstants.Wildcard:      // "*"
                    token = CreateToken(TokenKind.Wildcard, c, m_index++);
                    break;
                case '\'':
                    token = ReadStringToken();
                    break;
                case '!':   // "!" and "!="
                case '>':   // ">" and ">="
                case '<':   // "<" and "<="
                case '=':   // "=="
                case '&':   // "&&"
                case '|':   // "||"
                    token = ReadOperator();
                    break;
                default:
                    if (c == '.')
                    {
                        // Number
                        if (m_lastToken == null ||
                            m_lastToken.Kind == TokenKind.Separator ||          // ","
                            m_lastToken.Kind == TokenKind.StartGroup ||         // "(" logical grouping
                            m_lastToken.Kind == TokenKind.StartIndex ||         // "["
                            m_lastToken.Kind == TokenKind.StartParameters ||    // "(" function call
                            m_lastToken.Kind == TokenKind.LogicalOperator)      // "!", "==", etc
                        {
                            token = ReadNumberToken();
                        }
                        // "."
                        else
                        {
                            token = CreateToken(TokenKind.Dereference, c, m_index++);
                        }
                    }
                    else if (c == '-' || c == '+' || (c >= '0' && c <= '9'))
                    {
                        token = ReadNumberToken();
                    }
                    else
                    {
                        token = ReadKeywordToken();
                    }

                    break;
            }

            m_lastToken = token;
            return true;
        }

        private Token ReadNumberToken()
        {
            var startIndex = m_index;
            do
            {
                m_index++;
            }
            while (m_index < m_expression.Length && (!TestTokenBoundary(m_expression[m_index]) || m_expression[m_index] == '.'));

            var length = m_index - startIndex;
            var str = m_expression.Substring(startIndex, length);
            var d = ExpressionUtility.ParseNumber(str);

            if (Double.IsNaN(d))
            {
                return CreateToken(TokenKind.Unexpected, str, startIndex);
            }

            return CreateToken(TokenKind.Number, str, startIndex, d);
        }

        private Token ReadKeywordToken()
        {
            // Read to the end of the keyword.
            var startIndex = m_index;
            m_index++; // Skip the first char. It is already known to be the start of the keyword.
            while (m_index < m_expression.Length && !TestTokenBoundary(m_expression[m_index]))
            {
                m_index++;
            }

            // Test if valid keyword character sequence.
            var length = m_index - startIndex;
            var str = m_expression.Substring(startIndex, length);
            if (ExpressionUtility.IsLegalKeyword(str))
            {
                // Test if follows property dereference operator.
                if (m_lastToken != null && m_lastToken.Kind == TokenKind.Dereference)
                {
                    return CreateToken(TokenKind.PropertyName, str, startIndex);
                }

                // Null
                if (str.Equals(ExpressionConstants.Null, StringComparison.Ordinal))
                {
                    return CreateToken(TokenKind.Null, str, startIndex);
                }
                // Boolean
                else if (str.Equals(ExpressionConstants.True, StringComparison.Ordinal))
                {
                    return CreateToken(TokenKind.Boolean, str, startIndex, true);
                }
                else if (str.Equals(ExpressionConstants.False, StringComparison.Ordinal))
                {
                    return CreateToken(TokenKind.Boolean, str, startIndex, false);
                }
                // NaN
                else if (str.Equals(ExpressionConstants.NaN, StringComparison.Ordinal))
                {
                    return CreateToken(TokenKind.Number, str, startIndex, Double.NaN);
                }
                // Infinity
                else if (str.Equals(ExpressionConstants.Infinity, StringComparison.Ordinal))
                {
                    return CreateToken(TokenKind.Number, str, startIndex, Double.PositiveInfinity);
                }

                // Lookahead
                var tempIndex = m_index;
                while (tempIndex < m_expression.Length && Char.IsWhiteSpace(m_expression[tempIndex]))
                {
                    tempIndex++;
                }

                // Function
                if (tempIndex < m_expression.Length && m_expression[tempIndex] == ExpressionConstants.StartGroup)   // "("
                {
                    return CreateToken(TokenKind.Function, str, startIndex);
                }
                // Named-value
                else
                {
                    return CreateToken(TokenKind.NamedValue, str, startIndex);
                }
            }
            else
            {
                // Invalid keyword
                return CreateToken(TokenKind.Unexpected, str, startIndex);
            }
        }

        private Token ReadStringToken()
        {
            var startIndex = m_index;
            var c = default(Char);
            var closed = false;
            var str = new StringBuilder();
            m_index++; // Skip the leading single-quote.
            while (m_index < m_expression.Length)
            {
                c = m_expression[m_index++];
                if (c == '\'')
                {
                    // End of string.
                    if (m_index >= m_expression.Length || m_expression[m_index] != '\'')
                    {
                        closed = true;
                        break;
                    }

                    // Escaped single quote.
                    m_index++;
                }

                str.Append(c);
            }

            var length = m_index - startIndex;
            var rawValue = m_expression.Substring(startIndex, length);
            if (closed)
            {
                return CreateToken(TokenKind.String, rawValue, startIndex, str.ToString());
            }

            return CreateToken(TokenKind.Unexpected, rawValue, startIndex);
        }

        private Token ReadOperator()
        {
            var startIndex = m_index;
            var raw = default(String);
            m_index++;

            // Check for a two-character operator
            if (m_index < m_expression.Length)
            {
                m_index++;
                raw = m_expression.Substring(startIndex, 2);
                switch (raw)
                {
                    case ExpressionConstants.NotEqual:
                    case ExpressionConstants.GreaterThanOrEqual:
                    case ExpressionConstants.LessThanOrEqual:
                    case ExpressionConstants.Equal:
                    case ExpressionConstants.And:
                    case ExpressionConstants.Or:
                        return CreateToken(TokenKind.LogicalOperator, raw, startIndex);
                }

                // Backup
                m_index--;
            }

            // Check for one-character operator
            raw = m_expression.Substring(startIndex, 1);
            switch (raw)
            {
                case ExpressionConstants.Not:
                case ExpressionConstants.GreaterThan:
                case ExpressionConstants.LessThan:
                    return CreateToken(TokenKind.LogicalOperator, raw, startIndex);
            }

            // Unexpected
            while (m_index < m_expression.Length && !TestTokenBoundary(m_expression[m_index]))
            {
                m_index++;
            }

            var length = m_index - startIndex;
            raw = m_expression.Substring(startIndex, length);
            return CreateToken(TokenKind.Unexpected, raw, startIndex);
        }

        private static Boolean TestTokenBoundary(Char c)
        {
            switch (c)
            {
                case ExpressionConstants.StartGroup:    // "("
                case ExpressionConstants.StartIndex:    // "["
                case ExpressionConstants.EndGroup:      // ")"
                case ExpressionConstants.EndIndex:      // "]"
                case ExpressionConstants.Separator:     // ","
                case ExpressionConstants.Dereference:   // "."
                case '!': // "!" and "!="
                case '>': // ">" and ">="
                case '<': // "<" and "<="
                case '=': // "=="
                case '&': // "&&"
                case '|': // "||"
                    return true;
                default:
                    return char.IsWhiteSpace(c);
            }
        }

        private Token CreateToken(
            TokenKind kind,
            Char rawValue,
            Int32 index,
            Object parsedValue = null)
        {
            return CreateToken(kind, rawValue.ToString(), index, parsedValue);
        }

        private Token CreateToken(
            TokenKind kind,
            String rawValue,
            Int32 index,
            Object parsedValue = null)
        {
            // Check whether the current token is legal based on the last token
            var legal = false;
            switch (kind)
            {
                case TokenKind.StartGroup:      // "(" logical grouping
                    // Is first or follows "," or "(" or "[" or a logical operator
                    legal = CheckLastToken(null, TokenKind.Separator, TokenKind.StartGroup, TokenKind.StartParameters, TokenKind.StartIndex, TokenKind.LogicalOperator);
                    break;
                case TokenKind.StartIndex:      // "["
                    // Follows ")", "]", "*", a property name, or a named-value
                    legal = CheckLastToken(TokenKind.EndGroup, TokenKind.EndParameters, TokenKind.EndIndex, TokenKind.Wildcard, TokenKind.PropertyName, TokenKind.NamedValue);
                    break;
                case TokenKind.StartParameters: // "(" function call
                    // Follows a function
                    legal = CheckLastToken(TokenKind.Function);
                    break;
                case TokenKind.EndGroup:        // ")" logical grouping
                    // Follows ")", "]", "*", a literal, a property name, or a named-value
                    legal = CheckLastToken(TokenKind.EndGroup, TokenKind.EndParameters, TokenKind.EndIndex, TokenKind.Wildcard, TokenKind.Null, TokenKind.Boolean, TokenKind.Number, TokenKind.String, TokenKind.PropertyName, TokenKind.NamedValue);
                    break;
                case TokenKind.EndIndex:        // "]"
                    // Follows ")", "]", "*", a literal, a property name, or a named-value
                    legal = CheckLastToken(TokenKind.EndGroup, TokenKind.EndParameters, TokenKind.EndIndex, TokenKind.Wildcard, TokenKind.Null, TokenKind.Boolean, TokenKind.Number, TokenKind.String, TokenKind.PropertyName, TokenKind.NamedValue);
                    break;
                case TokenKind.EndParameters:   // ")" function call
                    // Follows "(" function call, ")", "]", "*", a literal, a property name, or a named-value
                    legal = CheckLastToken(TokenKind.StartParameters, TokenKind.EndGroup, TokenKind.EndParameters, TokenKind.EndIndex, TokenKind.Wildcard, TokenKind.Null, TokenKind.Boolean, TokenKind.Number, TokenKind.String, TokenKind.PropertyName, TokenKind.NamedValue);
                    break;
                case TokenKind.Separator:       // ","
                    // Follows ")", "]", "*", a literal, a property name, or a named-value
                    legal = CheckLastToken(TokenKind.EndGroup, TokenKind.EndParameters, TokenKind.EndIndex, TokenKind.Wildcard, TokenKind.Null, TokenKind.Boolean, TokenKind.Number, TokenKind.String, TokenKind.PropertyName, TokenKind.NamedValue);
                    break;
                case TokenKind.Dereference:     // "."
                    // Follows ")", "]", "*", a property name, or a named-value
                    legal = CheckLastToken(TokenKind.EndGroup, TokenKind.EndParameters, TokenKind.EndIndex, TokenKind.Wildcard, TokenKind.PropertyName, TokenKind.NamedValue);
                    break;
                case TokenKind.Wildcard:        // "*"
                    // Follows "[" or "."
                    legal = CheckLastToken(TokenKind.StartIndex, TokenKind.Dereference);
                    break;
                case TokenKind.LogicalOperator: // "!", "==", etc
                    switch (rawValue)
                    {
                        case ExpressionConstants.Not:
                            // Is first or follows "," or "(" or "[" or a logical operator
                            legal = CheckLastToken(null, TokenKind.Separator, TokenKind.StartGroup, TokenKind.StartParameters, TokenKind.StartIndex, TokenKind.LogicalOperator);
                            break;
                        default:
                            // Follows ")", "]", "*", a literal, a property name, or a named-value
                            legal = CheckLastToken(TokenKind.EndGroup, TokenKind.EndParameters, TokenKind.EndIndex, TokenKind.Wildcard, TokenKind.Null, TokenKind.Boolean, TokenKind.Number, TokenKind.String, TokenKind.PropertyName, TokenKind.NamedValue);
                            break;
                    }
                    break;
                case TokenKind.Null:
                case TokenKind.Boolean:
                case TokenKind.Number:
                case TokenKind.String:
                    // Is first or follows "," or "[" or "(" or a logical operator (e.g. "!" or "==" etc)
                    legal = CheckLastToken(null, TokenKind.Separator, TokenKind.StartIndex, TokenKind.StartGroup, TokenKind.StartParameters, TokenKind.LogicalOperator);
                    break;
                case TokenKind.PropertyName:
                    // Follows "."
                    legal = CheckLastToken(TokenKind.Dereference);
                    break;
                case TokenKind.Function:
                    // Is first or follows "," or "[" or "(" or a logical operator (e.g. "!" or "==" etc)
                    legal = CheckLastToken(null, TokenKind.Separator, TokenKind.StartIndex, TokenKind.StartGroup, TokenKind.StartParameters, TokenKind.LogicalOperator);
                    break;
                case TokenKind.NamedValue:
                    // Is first or follows "," or "[" or "(" or a logical operator (e.g. "!" or "==" etc)
                    legal = CheckLastToken(null, TokenKind.Separator, TokenKind.StartIndex, TokenKind.StartGroup, TokenKind.StartParameters, TokenKind.LogicalOperator);
                    break;
            }

            // Illegal
            if (!legal)
            {
                return new Token(TokenKind.Unexpected, rawValue, index);
            }

            // Legal so far
            var token = new Token(kind, rawValue, index, parsedValue);

            switch (kind)
            {
                case TokenKind.StartGroup:      // "(" logical grouping
                case TokenKind.StartIndex:      // "["
                case TokenKind.StartParameters: // "(" function call
                    // Track start token
                    m_unclosedTokens.Push(token);
                    break;

                case TokenKind.EndGroup:        // ")" logical grouping
                    // Check inside logical grouping
                    if (m_unclosedTokens.FirstOrDefault()?.Kind != TokenKind.StartGroup)
                    {
                        return new Token(TokenKind.Unexpected, rawValue, index);
                    }

                    // Pop start token
                    m_unclosedTokens.Pop();
                    break;

                case TokenKind.EndIndex:        // "]"
                    // Check inside indexer
                    if (m_unclosedTokens.FirstOrDefault()?.Kind != TokenKind.StartIndex)
                    {
                        return new Token(TokenKind.Unexpected, rawValue, index);
                    }

                    // Pop start token
                    m_unclosedTokens.Pop();
                    break;

                case TokenKind.EndParameters:   // ")" function call
                    // Check inside function call
                    if (m_unclosedTokens.FirstOrDefault()?.Kind != TokenKind.StartParameters)
                    {
                        return new Token(TokenKind.Unexpected, rawValue, index);
                    }

                    // Pop start token
                    m_unclosedTokens.Pop();
                    break;

                case TokenKind.Separator:       // ","
                    // Check inside function call
                    if (m_unclosedTokens.FirstOrDefault()?.Kind != TokenKind.StartParameters)
                    {
                        return new Token(TokenKind.Unexpected, rawValue, index);
                    }
                    break;
            }

            return token;
        }

        /// <summary>
        /// Checks whether the last token kind is in the array of allowed kinds.
        /// </summary>
        private Boolean CheckLastToken(params TokenKind?[] allowed)
        {
            var lastKind = m_lastToken?.Kind;
            foreach (var kind in allowed)
            {
                if (kind == lastKind)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly String m_expression; // Raw expression string
        private readonly Stack<Token> m_unclosedTokens = new Stack<Token>(); // Unclosed start tokens
        private Int32 m_index; // Index of raw expression string
        private Token m_lastToken;
    }
}
