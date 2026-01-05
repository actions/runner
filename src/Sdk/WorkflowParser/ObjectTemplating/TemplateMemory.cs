#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Actions.Expressions.Data;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;
using Newtonsoft.Json.Linq;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    /// <summary>
    /// Tracks characteristics about the current memory usage (CPU, stack, size)
    /// </summary>
    public sealed class TemplateMemory
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="maxBytes">The maximum allowed bytes</param>
        public TemplateMemory(Int32 maxBytes)
            : this(0, 0, maxBytes: maxBytes, null)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="maxDepth">The maximum allowed depth</param>
        /// <param name="maxEvents">The maximum allowed events</param>
        /// <param name="maxBytes">The maximum allowed bytes</param>
        internal TemplateMemory(
            Int32 maxDepth,
            Int32 maxEvents,
            Int32 maxBytes)
            : this(maxDepth, maxEvents, maxBytes, null)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="maxDepth">The maximum allowed depth</param>
        /// <param name="maxEvents">The maximum allowed events</param>
        /// <param name="maxBytes">The maximum allowed bytes</param>
        /// <param name="parent">Optional parent instance, for byte tracking only. Any bytes added/subtracted to the current instance, will be also added/subtracted to the parent instance.</param>
        internal TemplateMemory(
            Int32 maxDepth,
            Int32 maxEvents,
            Int32 maxBytes,
            TemplateMemory parent)
        {
            m_maxDepth = maxDepth;
            m_maxEvents = maxEvents;
            m_maxBytes = maxBytes;
            m_parent = parent;
        }

        public Int32 CurrentBytes => m_currentBytes;

        public Int32 MaxBytes => m_maxBytes;

        public void AddBytes(Int32 bytes)
        {
            checked
            {
                m_currentBytes += bytes;
            }

            if (m_currentBytes > m_maxBytes)
            {
                throw new InvalidOperationException(TemplateStrings.MaxObjectSizeExceeded());
            }

            m_parent?.AddBytes(bytes);
        }

        public void AddBytes(String value)
        {
            var bytes = CalculateBytes(value);
            AddBytes(bytes);
        }

        internal void AddBytes(
            ExpressionData value,
            Boolean traverse)
        {
            var bytes = CalculateBytes(value, traverse);
            AddBytes(bytes);
        }

        internal void AddBytes(
            JToken value,
            Boolean traverse)
        {
            var bytes = CalculateBytes(value, traverse);
            AddBytes(bytes);
        }

        internal void AddBytes(
            TemplateToken value,
            Boolean traverse = false)
        {
            var bytes = CalculateBytes(value, traverse);
            AddBytes(bytes);
        }

        internal void AddBytes(LiteralToken literal)
        {
            var bytes = CalculateBytes(literal);
            AddBytes(bytes);
        }

        internal void AddBytes(SequenceToken sequence)
        {
            var bytes = CalculateBytes(sequence);
            AddBytes(bytes);
        }

        internal void AddBytes(MappingToken mapping)
        {
            var bytes = CalculateBytes(mapping);
            AddBytes(bytes);
        }

        internal void AddBytes(BasicExpressionToken basicExpression)
        {
            var bytes = CalculateBytes(basicExpression);
            AddBytes(bytes);
        }

        internal void AddBytes(InsertExpressionToken insertExpression)
        {
            var bytes = CalculateBytes(insertExpression);
            AddBytes(bytes);
        }

        internal Int32 CalculateBytes(String value)
        {
            // This measurement doesn't have to be perfect
            // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/

            checked
            {
                return StringBaseOverhead + ((value?.Length ?? 0) * sizeof(Char));
            }
        }

        internal static Int32 CalculateBytes(
            ExpressionData value,
            Boolean traverse)
        {
            var enumerable = traverse ? value.Traverse() : new[] { value } as IEnumerable<ExpressionData>;
            var result = 0;
            foreach (var item in enumerable)
            {
                // This measurement doesn't have to be perfect
                // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/
                if (item is StringExpressionData str)
                {
                    checked
                    {
                        result += TemplateMemory.MinObjectSize + TemplateMemory.StringBaseOverhead + (str.Value.Length * sizeof(Char));
                    }
                }
                else if (item is ArrayExpressionData || item is DictionaryExpressionData || item is BooleanExpressionData || item is NumberExpressionData)
                {
                    // Min object size is good enough. Allows for base + a few fields.
                    checked
                    {
                        result += TemplateMemory.MinObjectSize;
                    }
                }
                else if (item is null)
                {
                    checked
                    {
                        result += IntPtr.Size;
                    }
                }
                else
                {
                    throw new NotSupportedException($"Unexpected workflow context data type '{item.GetType().Name}'");
                }
            }

            return result;
        }

        internal Int32 CalculateBytes(
            JToken value,
            Boolean traverse)
        {
            // This measurement doesn't have to be perfect
            // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/

            if (value is null)
            {
                return MinObjectSize;
            }

            if (!traverse)
            {
                switch (value.Type)
                {
                    case JTokenType.String:
                        checked
                        {
                            return StringBaseOverhead + (value.ToObject<String>().Length * sizeof(Char));
                        }

                    case JTokenType.Property:
                        var property = value as JProperty;
                        checked
                        {
                            return StringBaseOverhead + ((property.Name?.Length ?? 0) * sizeof(Char));
                        }

                    case JTokenType.Array:
                    case JTokenType.Boolean:
                    case JTokenType.Float:
                    case JTokenType.Integer:
                    case JTokenType.Null:
                    case JTokenType.Object:
                        return MinObjectSize;

                    default:
                        throw new NotSupportedException($"Unexpected JToken type '{value.Type}' when traversing object");
                }
            }

            var result = 0;
            do
            {
                // Descend as much as possible
                while (true)
                {
                    // Add bytes
                    var bytes = CalculateBytes(value, false);
                    checked
                    {
                        result += bytes;
                    }

                    // Descend
                    if (value.HasValues)
                    {
                        value = value.First;
                    }
                    // No more descendants
                    else
                    {
                        break;
                    }
                }

                // Next sibling or ancestor sibling
                do
                {
                    var sibling = value.Next;

                    // Sibling found
                    if (sibling != null)
                    {
                        value = sibling;
                        break;
                    }

                    // Ascend
                    value = value.Parent;

                } while (value != null);

            } while (value != null);

            return result;
        }

        internal Int32 CalculateBytes(
            TemplateToken value,
            Boolean traverse = false)
        {
            var enumerable = traverse ? value.Traverse() : new[] { value };
            var result = 0;
            foreach (var item in enumerable)
            {
                // This measurement doesn't have to be perfect
                // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/
                switch (item.Type)
                {
                    case TokenType.Null:
                    case TokenType.Boolean:
                    case TokenType.Number:
                        checked
                        {
                            result += MinObjectSize;
                        }
                        break;

                    case TokenType.String:
                        var stringToken = item as StringToken;
                        checked
                        {
                            result += MinObjectSize + StringBaseOverhead + ((stringToken.Value?.Length ?? 0) * sizeof(Char));
                        }
                        break;

                    case TokenType.Sequence:
                    case TokenType.Mapping:
                    case TokenType.InsertExpression:
                        // Min object size is good enough. Allows for base + a few fields.
                        checked
                        {
                            result += MinObjectSize;
                        }
                        break;

                    case TokenType.BasicExpression:
                        var basicExpression = item as BasicExpressionToken;
                        checked
                        {
                            result += MinObjectSize + StringBaseOverhead + ((basicExpression.Expression?.Length ?? 0) * sizeof(Char));
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Unexpected template type '{item.Type}'");
                }
            }

            return result;
        }

        internal void SubtractBytes(Int32 bytes)
        {
            if (bytes > m_currentBytes)
            {
                throw new InvalidOperationException("Bytes to subtract exceeds total bytes");
            }

            m_currentBytes -= bytes;

            m_parent?.SubtractBytes(bytes);
        }

        internal void SubtractBytes(
            TemplateToken value,
            Boolean traverse = false)
        {
            var bytes = CalculateBytes(value, traverse);
            SubtractBytes(bytes);
        }

        internal void IncrementDepth()
        {
            if (m_depth++ >= m_maxDepth)
            {
                throw new InvalidOperationException(TemplateStrings.MaxObjectDepthExceeded());
            }
        }

        internal void DecrementDepth()
        {
            m_depth--;
        }

        internal void IncrementEvents()
        {
            if (m_events++ >= m_maxEvents)
            {
                throw new InvalidOperationException(TemplateStrings.MaxTemplateEventsExceeded());
            }
        }

        internal const Int32 MinObjectSize = 24;
        internal const Int32 StringBaseOverhead = 26;
        private readonly Int32 m_maxDepth;
        private readonly Int32 m_maxEvents;
        private readonly Int32 m_maxBytes;
        private Int32 m_depth;
        private Int32 m_events;
        private Int32 m_currentBytes;
        private TemplateMemory m_parent;
    }
}
