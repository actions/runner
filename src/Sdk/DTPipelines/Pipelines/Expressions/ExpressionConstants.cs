﻿using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions;

namespace GitHub.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExpressionConstants
    {
        /// <summary>
        /// Gets the name of the variables node.
        /// </summary>
        public static readonly String Variables = "variables";

        /// <summary>
        /// Gets the pipeline context available in pipeline expressions.
        /// </summary>
        public static readonly INamedValueInfo PipelineNamedValue = new NamedValueInfo<PipelineContextNode>("pipeline");

        /// <summary>
        /// Gets the variable context available in pipeline expressions.
        /// </summary>
        public static readonly INamedValueInfo VariablesNamedValue = new NamedValueInfo<VariablesContextNode>("variables");

        /// <summary>
        /// Gets the counter function available in pipeline expressions.
        /// </summary>
        public static readonly IFunctionInfo CounterFunction = new FunctionInfo<CounterNode>("counter", 0, 2);
    }
}
