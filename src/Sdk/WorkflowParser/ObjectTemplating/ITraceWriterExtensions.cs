namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    internal static class ITraceWriterExtensions
    {
        internal static GitHub.Actions.Expressions.ITraceWriter ToExpressionTraceWriter(this ITraceWriter trace)
        {
            return new ExpressionTraceWriter(trace);
        }
    }
}
