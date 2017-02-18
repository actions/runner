// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Expressions
// Repo 2) vsts-agent repo on GitHub under src/Microsoft.VisualStudio.Services.Agent/DistributedTask.Expressions
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions
{
    internal sealed class LexicalAnalyzer
    {
        public LexicalAnalyzer(String expression, ITraceWriter trace, IEnumerable<String> namedValues, IEnumerable<String> functions)
        {
            if (trace == null)
            {
                throw new ArgumentNullException(nameof(trace));
            }

            m_expression = expression;
            m_trace = trace;
            m_extensionNamedValues = new HashSet<String>(namedValues ?? new String[0], StringComparer.OrdinalIgnoreCase);
            m_extensionFunctions = new HashSet<String>(functions ?? new String[0], StringComparer.OrdinalIgnoreCase);
        }

        public Boolean TryGetNextToken(ref Token token)
        {
            // Skip whitespace.
            while (m_index < m_expression.Length && Char.IsWhiteSpace(m_expression[m_index]))
            {
                m_index++;
            }

            // Test end of string.
            if (m_index >= m_expression.Length)
            {
                token = null;
                return false;
            }

            // Read the first character to determine the type of token.
            Char c = m_expression[m_index];
            switch (c)
            {
                case StartIndex:
                    token = new Token(TokenKind.StartIndex, c, m_index++);
                    break;
                case StartParameter:
                    token = new Token(TokenKind.StartParameter, c, m_index++);
                    break;
                case EndIndex:
                    token = new Token(TokenKind.EndIndex, c, m_index++);
                    break;
                case EndParameter:
                    token = new Token(TokenKind.EndParameter, c, m_index++);
                    break;
                case Separator:
                    token = new Token(TokenKind.Separator, c, m_index++);
                    break;
                case '\'':
                    token = ReadStringToken();
                    break;
                default:
                    if (c == '.')
                    {
                        if (m_lastToken == null ||
                            m_lastToken.Kind == TokenKind.Separator ||
                            m_lastToken.Kind == TokenKind.StartIndex ||
                            m_lastToken.Kind == TokenKind.StartParameter)
                        {
                            token = ReadNumberOrVersionToken();
                        }
                        else
                        {
                            token = new Token(TokenKind.Dereference, c, m_index++);
                        }
                    }
                    else if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        token = ReadNumberOrVersionToken();
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

        private Token ReadNumberOrVersionToken()
        {
            Int32 startIndex = m_index;
            Int32 periods = 0;
            do
            {
                if (m_expression[m_index] == '.')
                {
                    periods++;
                }

                m_index++;
            }
            while (m_index < m_expression.Length && (!TestWhitespaceOrPunctuation(m_expression[m_index]) || m_expression[m_index] == '.'));

            Int32 length = m_index - startIndex;
            String str = m_expression.Substring(startIndex, length);
            if (periods >= 2)
            {
                Version version;
                if (Version.TryParse(str, out version))
                {
                    return new Token(TokenKind.Version, str, startIndex, version);
                }
            }
            else
            {
                // Note, NumberStyles.AllowThousands cannot be allowed since comma has special meaning as a token separator.
                Decimal d;
                if (Decimal.TryParse(
                        str,
                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture,
                        out d))
                {
                    return new Token(TokenKind.Number, str, startIndex, d);
                }
            }

            return new Token(TokenKind.Unrecognized, str, startIndex);
        }

        private Token ReadKeywordToken()
        {
            // Read to the end of the keyword.
            Int32 startIndex = m_index;
            m_index++; // Skip the first char. It is already known to be the start of the keyword.
            while (m_index < m_expression.Length && !TestWhitespaceOrPunctuation(m_expression[m_index]))
            {
                m_index++;
            }

            // Test if valid keyword character sequence.
            Int32 length = m_index - startIndex;
            String str = m_expression.Substring(startIndex, length);
            if (s_keywordRegex.IsMatch(str))
            {
                // Test if follows property dereference operator.
                if (m_lastToken != null && m_lastToken.Kind == TokenKind.Dereference)
                {
                    return new Token(TokenKind.PropertyName, str, startIndex);
                }

                // Boolean
                if (str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    return new Token(TokenKind.Boolean, str, startIndex, true);
                }
                else if (str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    return new Token(TokenKind.Boolean, str, startIndex, false);
                }
                // Well-known function
                else if (ExpressionConstants.WellKnownFunctions.ContainsKey(str))
                {
                    return new Token(TokenKind.WellKnownFunction, str, startIndex);
                }
                // Extension value
                else if (m_extensionNamedValues.Contains(str))
                {
                    return new Token(TokenKind.ExtensionNamedValue, str, startIndex);
                }
                // Extension function
                else if (m_extensionFunctions.Contains(str))
                {
                    return new Token(TokenKind.ExtensionFunction, str, startIndex);
                }
            }

            // Unrecognized
            return new Token(TokenKind.Unrecognized, str, startIndex);
        }

        private Token ReadStringToken()
        {
            Int32 startIndex = m_index;
            Char c;
            Boolean closed = false;
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

            Int32 length = m_index - startIndex;
            String rawValue = m_expression.Substring(startIndex, length);
            if (closed)
            {
                return new Token(TokenKind.String, rawValue, startIndex, str.ToString());
            }

            return new Token(TokenKind.Unrecognized, rawValue, startIndex);
        }

        private static Boolean TestWhitespaceOrPunctuation(Char c)
        {
            switch (c)
            {
                case StartIndex:
                case StartParameter:
                case EndIndex:
                case EndParameter:
                case Separator:
                case Dereference:
                    return true;
                default:
                    return char.IsWhiteSpace(c);
            }
        }

        // Punctuation
        private const Char StartIndex = '[';
        private const Char StartParameter = '(';
        private const Char EndIndex = ']';
        private const Char EndParameter = ')';
        private const Char Separator = ',';
        private const Char Dereference = '.';

        private static readonly Regex s_keywordRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.None);
        private readonly String m_expression; // Raw expression string.
        private readonly ITraceWriter m_trace;
        private readonly HashSet<String> m_extensionFunctions;
        private readonly HashSet<String> m_extensionNamedValues;
        private Int32 m_index; // Index of raw condition string.
        private Token m_lastToken;
    }
}
