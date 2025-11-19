using System;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    /// <summary>
    /// Wraps an ITraceWriter so it can be passed for expression evaluation.
    /// </summary>
    internal sealed class ExpressionTraceWriter : GitHub.Actions.Expressions.ITraceWriter
    {
        public ExpressionTraceWriter(ITraceWriter trace)
        {
            m_trace = trace;
        }

        public void Info(String message)
        {
            m_trace.Info("{0}", message);
        }

        public void Verbose(String message)
        {
            m_trace.Verbose("{0}", message);
        }

        private readonly ITraceWriter m_trace;
    }
}
