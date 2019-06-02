using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Logging;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IExpressionNode
    {
        /// <summary>
        /// Evaluates the expression and attempts to cast or deserialize the result to the specified
        /// type. The specified type can either be simple type or a JSON-serializable class. Allowed
        /// simple types are: Boolean, String, Version, Byte, SByte, Int16, UInt16, Int32, UInt32,
        /// Int64, UInt64, Single, Double, or Decimal. When a JSON-serializable class is specified, the
        /// following rules are applied: If the type of the evaluation result object, is assignable to
        /// the specified type, then the result will be cast and returned. If the evaluation result
        /// object is a String, it will be deserialized as the specified type. If the evaluation result
        /// object is null, null will be returned.
        /// </summary>
        /// <param name="trace">Optional trace writer</param>
        /// <param name="secretMasker">Optional secret masker</param>
        /// <param name="state">State object for custom evaluation function nodes and custom named-value nodes</param>
        T Evaluate<T>(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options = null);

        /// <summary>
        /// Evaluates the expression and returns the result.
        /// </summary>
        /// <param name="trace">Optional trace writer</param>
        /// <param name="secretMasker">Optional secret masker</param>
        /// <param name="state">State object for custom evaluation function nodes and custom named-value nodes</param>
        Object Evaluate(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options = null);

        /// <summary>
        /// Evaluates the expression and casts the result to a Boolean.
        /// </summary>
        /// <param name="trace">Optional trace writer</param>
        /// <param name="secretMasker">Optional secret masker</param>
        /// <param name="state">State object for custom evaluation function nodes and custom named-value nodes</param>
        Boolean EvaluateBoolean(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state);

        IEnumerable<T> GetParameters<T>() where T : IExpressionNode;

        /// <summary>
        /// Evaluates the expression and returns the result, wrapped in the SDK helper
        /// for converting, comparing, and traversing objects.
        /// </summary>
        /// <param name="trace">Optional trace writer</param>
        /// <param name="secretMasker">Optional secret masker</param>
        /// <param name="state">State object for custom evaluation function nodes and custom named-value nodes</param>
        /// <param name="options">Evaluation options</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        EvaluationResult EvaluateResult(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options);
    }
}
