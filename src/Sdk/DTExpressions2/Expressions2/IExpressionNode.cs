using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Logging;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IExpressionNode
    {
        /// <summary>
        /// Evaluates the expression and returns the result, wrapped in a helper
        /// for converting, comparing, and traversing objects.
        /// </summary>
        /// <param name="trace">Optional trace writer</param>
        /// <param name="secretMasker">Optional secret masker</param>
        /// <param name="state">State object for custom evaluation function nodes and custom named-value nodes</param>
        /// <param name="options">Evaluation options</param>
        EvaluationResult Evaluate(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options);
    }
}
