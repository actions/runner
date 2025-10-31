#nullable enable

using System;
using GitHub.Actions.Expressions;
using GitHub.Actions.Expressions.Sdk;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    /// <summary>
    /// Feature usage data
    /// </summary>
    internal class WorkflowUsage
    {
        public WorkflowUsage(IServerTraceWriter? serverTrace)
        {
            m_serverTrace = serverTrace;
        }

        /// <summary>
        /// Gathers feature usage from template tokens. Call <c ref="Trace" /> after gathering all data.
        /// </summary>
        public void Gather(
            TemplateContext context,
            TemplateToken token)
        {
            try
            {
                if (context.Errors.Count > 0 || context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                foreach (var t in token.Traverse())
                {
                    // ${{ insert }}
                    if (t is InsertExpressionToken)
                    {
                        m_data.ContainsInsertExpression = true;
                    }
                    else if (t is BasicExpressionToken expressionToken)
                    {
                        // Parse the expression
                        var parser = new ExpressionParser();
                        var tree = parser.ValidateSyntax(expressionToken.Expression, null);
                        foreach (var node in IExpressionNodeExtensions.Traverse(tree))
                        {
                            // success(arg[, arg]) or failure(arg[, arg])
                            if (node is Function functionNode &&
                                (string.Equals(functionNode.Name, "success", StringComparison.OrdinalIgnoreCase) || string.Equals(functionNode.Name, "failure", StringComparison.OrdinalIgnoreCase)) &&
                                functionNode.Parameters.Count > 0)
                            {
                                m_data.CallsSuccessOrFailureWithArgs = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_serverTrace?.TraceAlways(c_yamlFeaturesTracepoint, "Unexpected exception when gathering telemetry from YAML: {0}", ex);
            }
        }

        /// <summary>
        /// Traces feature usage. Call this method once after gathering all data.
        /// </summary>
        public void Trace()
        {
            if (m_data.CallsSuccessOrFailureWithArgs)
            {
                m_serverTrace?.TraceAlways(c_yamlFeaturesTracepoint, "CallsSuccessOrFailureWithArgs=true");
            }

            if (m_data.ContainsInsertExpression)
            {
                m_serverTrace?.TraceAlways(c_yamlFeaturesTracepoint, "ContainsInsertExpression=true");
            }

            m_data = new UsageData();
        }

        private sealed class UsageData
        {
            public bool CallsSuccessOrFailureWithArgs { get; set; }
            public bool ContainsInsertExpression { get; set; }
        }

        private const int c_yamlFeaturesTracepoint = 10016155; // Copied from /Actions/Runtime/Sdk/Server/TraceConstants.cs
        private readonly IServerTraceWriter? m_serverTrace;
        private UsageData m_data = new UsageData();
    }
}
