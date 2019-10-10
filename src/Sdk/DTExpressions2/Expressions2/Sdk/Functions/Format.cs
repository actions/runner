using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ExpressionResources = GitHub.DistributedTask.Expressions.ExpressionResources;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class Format : Function
    {
        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var format = Parameters[0].Evaluate(context).ConvertToString();
            var index = 0;
            var result = new FormatResultBuilder(this, context, CreateMemoryCounter(context));
            while (index < format.Length)
            {
                var lbrace = format.IndexOf('{', index);
                var rbrace = format.IndexOf('}', index);

                // Left brace
                if (lbrace >= 0 && (rbrace < 0 || rbrace > lbrace))
                {
                    // Escaped left brace
                    if (SafeCharAt(format, lbrace + 1) == '{')
                    {
                        result.Append(format.Substring(index, lbrace - index + 1));
                        index = lbrace + 2;
                    }
                    // Left brace, number, optional format specifiers, right brace
                    else if (rbrace > lbrace + 1 &&
                        ReadArgIndex(format, lbrace + 1, out Byte argIndex, out Int32 endArgIndex) &&
                        ReadFormatSpecifiers(format, endArgIndex + 1, out String formatSpecifiers, out rbrace))
                    {
                        // Check parameter count
                        if (argIndex > Parameters.Count - 2)
                        {
                            throw new FormatException(ExpressionResources.InvalidFormatArgIndex(format));
                        }

                        // Append the portion before the left brace
                        if (lbrace > index)
                        {
                            result.Append(format.Substring(index, lbrace - index));
                        }

                        // Append the arg
                        result.Append(argIndex, formatSpecifiers);
                        index = rbrace + 1;
                    }
                    else
                    {
                        throw new FormatException(ExpressionResources.InvalidFormatString(format));
                    }
                }
                // Right brace
                else if (rbrace >= 0)
                {
                    // Escaped right brace
                    if (SafeCharAt(format, rbrace + 1) == '}')
                    {
                        result.Append(format.Substring(index, rbrace - index + 1));
                        index = rbrace + 2;
                    }
                    else
                    {
                        throw new FormatException(ExpressionResources.InvalidFormatString(format));
                    }
                }
                // Last segment
                else
                {
                    result.Append(format.Substring(index));
                    break;
                }
            }

            return result.ToString();
        }

        private Boolean ReadArgIndex(
            String str,
            Int32 startIndex,
            out Byte result,
            out Int32 endIndex)
        {
            // Count the number of digits
            var length = 0;
            while (Char.IsDigit(SafeCharAt(str, startIndex + length)))
            {
                length++;
            }

            // Validate at least one digit
            if (length < 1)
            {
                result = default;
                endIndex = default;
                return false;
            }

            // Parse the number
            endIndex = startIndex + length - 1;
            return Byte.TryParse(str.Substring(startIndex, length), NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        private Boolean ReadFormatSpecifiers(
            String str,
            Int32 startIndex,
            out String result,
            out Int32 rbrace)
        {
            // No format specifiers
            var c = SafeCharAt(str, startIndex);
            if (c == '}')
            {
                result = String.Empty;
                rbrace = startIndex;
                return true;
            }

            // Validate starts with ":"
            if (c != ':')
            {
                result = default;
                rbrace = default;
                return false;
            }

            // Read the specifiers
            var specifiers = new StringBuilder();
            var index = startIndex + 1;
            while (true)
            {
                // Validate not the end of the string
                if (index >= str.Length)
                {
                    result = default;
                    rbrace = default;
                    return false;
                }

                c = str[index];

                // Not right-brace
                if (c != '}')
                {
                    specifiers.Append(c);
                    index++;
                }
                // Escaped right-brace
                else if (SafeCharAt(str, index + 1) == '}')
                {
                    specifiers.Append('}');
                    index += 2;
                }
                // Closing right-brace
                else
                {
                    result = specifiers.ToString();
                    rbrace = index;
                    return true;
                }
            }
        }

        private Char SafeCharAt(
            String str,
            Int32 index)
        {
            if (str.Length > index)
            {
                return str[index];
            }

            return '\0';
        }

        private sealed class FormatResultBuilder
        {
            internal FormatResultBuilder(
                Format node,
                EvaluationContext context,
                MemoryCounter counter)
            {
                m_node = node;
                m_context = context;
                m_counter = counter;
                m_cache = new ArgValue[node.Parameters.Count - 1];
            }

            // Build the final string. This is when lazy segments are evaluated.
            public override String ToString()
            {
                return String.Join(
                    String.Empty,
                    m_segments.Select(obj =>
                    {
                        if (obj is Lazy<String> lazy)
                        {
                            return lazy.Value;
                        }
                        else
                        {
                            return obj as String;
                        }
                    }));
            }

            // Append a static value
            internal void Append(String value)
            {
                if (value?.Length > 0)
                {
                    // Track memory
                    m_counter.Add(value);

                    // Append the segment
                    m_segments.Add(value);
                }
            }

            // Append an argument
            internal void Append(
                Int32 argIndex,
                String formatSpecifiers)
            {
                // Delay execution until the final ToString
                m_segments.Add(new Lazy<String>(() =>
                {
                    String result;

                    // Get the arg from the cache
                    var argValue = m_cache[argIndex];

                    // Evaluate the arg and cache the result
                    if (argValue == null)
                    {
                        // The evaluation result is required when format specifiers are used. Otherwise the string
                        // result is required. Go ahead and store both values. Since ConvertToString produces tracing,
                        // we need to run that now so the tracing appears in order in the log.
                        var evaluationResult = m_node.Parameters[argIndex + 1].Evaluate(m_context);
                        var stringResult = evaluationResult.ConvertToString();
                        argValue = new ArgValue(evaluationResult, stringResult);
                        m_cache[argIndex] = argValue;
                    }

                    // No format specifiers
                    if (String.IsNullOrEmpty(formatSpecifiers))
                    {
                        result = argValue.StringResult;
                    }
                    // Invalid
                    else
                    {
                        throw new FormatException(ExpressionResources.InvalidFormatSpecifiers(formatSpecifiers, argValue.EvaluationResult.Kind));
                    }

                    // Track memory
                    if (!String.IsNullOrEmpty(result))
                    {
                        m_counter.Add(result);
                    }

                    return result;
                }));
            }

            private readonly ArgValue[] m_cache;
            private readonly EvaluationContext m_context;
            private readonly MemoryCounter m_counter;
            private readonly Format m_node;
            private readonly List<Object> m_segments = new List<Object>();
        }

        /// <summary>
        /// Stores an EvaluateResult and the value converted to a String.
        /// </summary>
        private sealed class ArgValue
        {
            public ArgValue(
                EvaluationResult evaluationResult,
                String stringResult)
            {
                EvaluationResult = evaluationResult;
                StringResult = stringResult;
            }

            public EvaluationResult EvaluationResult { get; }

            public String StringResult { get; }
        }
    }
}
