﻿using System;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Expressions
{
    /// <summary>
    /// Helper class for ExpressionNode authors. This class helps calculate memory overhead for a result object.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class MemoryCounter
    {
        internal MemoryCounter(
            ExpressionNode node,
            Int32? maxBytes)
        {
            m_node = node;
            m_maxBytes = (maxBytes ?? 0) > 0 ? maxBytes.Value : Int32.MaxValue;
        }

        public Int32 CurrentBytes => m_currentBytes;

        public void Add(Int32 amount)
        {
            if (!TryAdd(amount))
            {
                throw new InvalidOperationException(ExpressionResources.ExceededAllowedMemory(m_node?.ConvertToExpression()));
            }
        }

        public void Add(String value)
        {
            Add(CalculateSize(value));
        }

        public void Add(
            JToken value,
            Boolean traverse)
        {
            // This measurement doesn't have to be perfect
            // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/

            if (value is null)
            {
                Add(MinObjectSize);
            }

            if (!traverse)
            {
                switch (value.Type)
                {
                    case JTokenType.Bytes:
                    case JTokenType.String:
                    case JTokenType.Uri:
                        Add(value.ToObject<String>());
                        return;

                    case JTokenType.Property:
                        var property = value as JProperty;
                        Add(property.Name);
                        return;

                    default:
                        Add(MinObjectSize);
                        return;
                }
            }

            do
            {
                // Descend as much as possible
                while (true)
                {
                    // Add bytes
                    Add(value, false);

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
        }

        public void AddMinObjectSize()
        {
            Add(MinObjectSize);
        }

        public void Remove(String value)
        {
            m_currentBytes -= CalculateSize(value);
        }

        public static Int32 CalculateSize(String value)
        {
            // This measurement doesn't have to be perfect.
            // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/

            Int32 bytes;
            checked
            {
                bytes = StringBaseOverhead + ((value?.Length ?? 0) * 2);
            }
            return bytes;
        }

        internal Boolean TryAdd(Int32 amount)
        {
            try
            {
                checked
                {
                    amount += m_currentBytes;
                }

                if (amount > m_maxBytes)
                {
                    return false;
                }

                m_currentBytes = amount;
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        internal Boolean TryAdd(String value)
        {
            return TryAdd(CalculateSize(value));
        }

        internal const Int32 MinObjectSize = 24;
        internal const Int32 StringBaseOverhead = 26;
        private readonly Int32 m_maxBytes;
        private readonly ExpressionNode m_node;
        private Int32 m_currentBytes;
    }
}
