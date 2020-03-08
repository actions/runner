using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    /// <summary>
    /// Base class for all template expression tokens
    /// </summary>
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ExpressionToken : ScalarToken
    {
        internal ExpressionToken(
            Int32 templateType,
            Int32? fileId,
            Int32? line,
            Int32? column,
            String directive)
            : base(templateType, fileId, line, column)
        {
            Directive = directive;
        }

        [DataMember(Name = "directive", EmitDefaultValue = false)]
        internal String Directive { get; }

        internal static Boolean IsValidExpression(
            String expression,
            String[] allowedContext,
            out Exception ex)
        {
            // Create dummy named values and functions
            var namedValues = new List<INamedValueInfo>();
            var functions = new List<IFunctionInfo>();
            if (allowedContext?.Length > 0)
            {
                foreach (var contextItem in allowedContext)
                {
                    var match = s_function.Match(contextItem);
                    if (match.Success)
                    {
                        var functionName = match.Groups[1].Value;
                        var minParameters = Int32.Parse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                        var maxParametersRaw = match.Groups[3].Value;
                        var maxParameters = String.Equals(maxParametersRaw, TemplateConstants.MaxConstant, StringComparison.Ordinal)
                            ? Int32.MaxValue
                            : Int32.Parse(maxParametersRaw, NumberStyles.None, CultureInfo.InvariantCulture);
                        functions.Add(new FunctionInfo<DummyFunction>(functionName, minParameters, maxParameters));
                    }
                    else
                    {
                        namedValues.Add(new NamedValueInfo<ContextValueNode>(contextItem));
                    }
                }
            }

            // Parse
            Boolean result;
            ExpressionNode root = null;
            try
            {
                root = new ExpressionParser().CreateTree(expression, null, namedValues, functions) as ExpressionNode;

                result = true;
                ex = null;
            }
            catch (Exception exception)
            {
                result = false;
                ex = exception;
            }

            return result;
        }

        private sealed class DummyFunction : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                return null;
            }
        }

        private static readonly Regex s_function = new Regex(@"^([a-zA-Z0-9_]+)\(([0-9]+),([0-9]+|MAX)\)$", RegexOptions.Compiled);
    }
}
