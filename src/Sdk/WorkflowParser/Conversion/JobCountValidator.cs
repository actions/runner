#nullable enable

using System;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    internal sealed class JobCountValidator
    {
        public JobCountValidator(
            TemplateContext context,
            Int32 maxCount)
        {
            m_context = context ?? throw new ArgumentNullException(nameof(context));
            m_maxCount = maxCount;
        }

        /// <summary>
        /// Increments the job counter.
        ///
        /// Appends an error to the template context only when the max job count is initially exceeded.
        /// Additional calls will not append more errors.
        /// </summary>
        /// <param name="token">The token to use for error reporting.</param>
        public void Increment(TemplateToken? token)
        {
            // Initial breach?
            if (m_maxCount > 0 &&
                m_count + 1 > m_maxCount &&
                m_count <= m_maxCount)
            {
                m_context.Error(token, $"Workflows may not contain more than {m_maxCount} jobs across all referenced files");
            }

            // Increment
            m_count++;
        }

        private readonly TemplateContext m_context;
        private readonly Int32 m_maxCount;
        private Int32 m_count;
    }
}
