using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
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
            // Create dummy allowed contexts
            INamedValueInfo[] namedValues = null;
            if (allowedContext?.Length > 0)
            {
                namedValues = allowedContext.Select(x => new NamedValueInfo<ContextValueNode>(x)).ToArray();
            }

            // Parse
            Boolean result;
            ExpressionNode root = null;
            try
            {
                root = new ExpressionParser().CreateTree(expression, null, namedValues, null) as ExpressionNode;

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

        internal static List<NamedValue> GetExpressionNamedValues(
            String expression,
            out Exception ex)
        {
            ExpressionNode root = null;
            List<NamedValue> result = null;
            try
            {
                root = new ExpressionParser().ValidateSyntax(expression, null) as ExpressionNode;
                result = root.Traverse().OfType<NamedValue>().ToList();
                ex = null;
            }
            catch (Exception exception)
            {
                result = new List<NamedValue>();
                ex = exception;
            }

            return result;
        }
    }
}
