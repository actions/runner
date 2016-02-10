using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public static class ExecutionContextExtension
    {
        public static void LogException(this IExecutionContext context, Exception ex)
        {
            context.LogMessage(LogLevel.Error, ex.Message);
            context.LogMessage(LogLevel.Verbose, ex.ToString());
        }

        public static void LogError(this IExecutionContext context, String format, params Object[] args)
        {
            context.LogMessage(LogLevel.Error, format, args);
        }

        public static void LogWarning(this IExecutionContext context, String format, params Object[] args)
        {
            context.LogMessage(LogLevel.Warning, format, args);
        }

        public static void LogInfo(this IExecutionContext context, String format, params Object[] args)
        {
            context.LogMessage(LogLevel.Info, format, args);
        }

        public static void LogVerbose(this IExecutionContext context, String format, params Object[] args)
        {
            context.LogMessage(LogLevel.Verbose, format, args);
        }
    }
}