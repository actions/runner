using System;
using System.Collections.Generic;
using ExpressionResources = GitHub.DistributedTask.Expressions.ExpressionResources;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    /// <summary>
    /// This is an internal class only.
    ///
    /// This class is used to track current memory consumption
    /// across the entire expression evaluation.
    /// </summary>
    internal sealed class EvaluationMemory
    {
        internal EvaluationMemory(
            Int32 maxBytes,
            ExpressionNode node)
        {
            m_maxAmount = maxBytes;
            m_node = node;
        }

        internal void AddAmount(
            Int32 depth,
            Int32 bytes,
            Boolean trimDepth = false)
        {
            // Trim deeper depths
            if (trimDepth)
            {
                while (m_maxActiveDepth > depth)
                {
                    var amount = m_depths[m_maxActiveDepth];

                    if (amount > 0)
                    {
                        // Sanity check
                        if (amount > m_totalAmount)
                        {
                            throw new InvalidOperationException("Bytes to subtract exceeds total bytes");
                        }

                        // Subtract from the total
                        checked
                        {
                            m_totalAmount -= amount;
                        }

                        // Reset the amount
                        m_depths[m_maxActiveDepth] = 0;
                    }

                    m_maxActiveDepth--;
                }
            }

            // Grow the depths
            if (depth > m_maxActiveDepth)
            {
                // Grow the list
                while (m_depths.Count <= depth)
                {
                    m_depths.Add(0);
                }

                // Adjust the max active depth
                m_maxActiveDepth = depth;
            }

            checked
            {
                // Add to the depth
                m_depths[depth] += bytes;

                // Add to the total
                m_totalAmount += bytes;
            }

            // Check max
            if (m_totalAmount > m_maxAmount)
            {
                throw new InvalidOperationException(ExpressionResources.ExceededAllowedMemory(m_node?.ConvertToExpression()));
            }
        }

        internal static Int32 CalculateBytes(Object obj)
        {
            if (obj is String str)
            {
                // This measurement doesn't have to be perfect
                // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/

                checked
                {
                    return c_stringBaseOverhead + ((str?.Length ?? 0) * sizeof(Char));
                }
            }
            else
            {
                return c_minObjectSize;
            }
        }

        private const Int32 c_minObjectSize = 24;
        private const Int32 c_stringBaseOverhead = 26;
        private readonly List<Int32> m_depths = new List<Int32>();
        private readonly Int32 m_maxAmount;
        private readonly ExpressionNode m_node;
        private Int32 m_maxActiveDepth = -1;
        private Int32 m_totalAmount;
    }
}
