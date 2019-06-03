namespace GitHub.DistributedTask.ObjectTemplating
{
    internal static class ITraceWriterExtensions
    {
        internal static DistributedTask.Expressions.ITraceWriter ToExpressionTraceWriter(this ITraceWriter trace)
        {
            return new ExpressionTraceWriter(trace);
        }
    }
}
