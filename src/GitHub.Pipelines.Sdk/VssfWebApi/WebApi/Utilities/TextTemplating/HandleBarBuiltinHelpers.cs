using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Static helper class for handlebar default/builtin template helpers
    /// </summary>
    internal static class HandleBarBuiltinHelpers
    {
        internal static Dictionary<String, MustacheTemplateHelperWriter> GetHelpers()
        {
            return new Dictionary<string, MustacheTemplateHelperWriter>(StringComparer.OrdinalIgnoreCase)
            {
                { ">", HandlebarPartialHelper },
                { "with", HandlebarBlockWithHelper },
                { "if", HandlebarBlockIfHelper },
                { "else", HandlebarBlockUnlessHelper },
                { "unless", HandlebarBlockUnlessHelper },
                { "each", HandlebarBlockEachHelper },
                { "lookup", HandlebarBlockLookupHelper }
            };
        }

        /// <summary>
        /// {{#with ...}} block helper sets context for the child expressions
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static void HandlebarBlockWithHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            JToken replacementObject = expression.GetCurrentJToken(expression.Expression, context);
            if (expression.IsTokenTruthy(replacementObject))
            {
                MustacheEvaluationContext newContext = new MustacheEvaluationContext()
                {
                    ParentContext = context,
                    ReplacementObject = replacementObject,
                    PartialExpressions = context.PartialExpressions,
                    AdditionalEvaluationData = context.AdditionalEvaluationData,
                    Options = context.Options
                };
                MustacheEvaluationContext.CombinePartialsDictionaries(context, expression);
                expression.EvaluateChildExpressions(newContext, writer);
            }
        }

        /// <summary>
        /// {{#if ...}} block helper evaluates child expressions ONLY if the selected value is true
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static void HandlebarBlockIfHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            JToken replacementObject = expression.GetCurrentJToken(expression.Expression, context);
            if (!expression.IsTokenTruthy(replacementObject))
            {
                if (!expression.IsBlockExpression)
                {
                    writer.Write("false");
                }
            }
            else
            {
                if (expression.IsBlockExpression)
                {
                    expression.EvaluateChildExpressions(context, writer);
                }
                else
                {
                    writer.Write("true");
                }
            }
        }

        /// <summary>
        /// {{#unless ...}} block helper evaluates child expressions ONLY if the selected value is false
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static void HandlebarBlockUnlessHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            JToken replacementObject = expression.GetCurrentJToken(expression.Expression, context);
            if (expression.IsTokenTruthy(replacementObject))
            {
                if (!expression.IsBlockExpression)
                {
                    writer.Write("false");
                }
            }
            else
            {
                if (expression.IsBlockExpression)
                {
                    expression.EvaluateChildExpressions(context, writer);
                }
                else
                {
                    writer.Write("true");
                }
            }
        }

        /// <summary>
        /// {{#each ...}} block helper evaluates child expressions once for every item in an array or object
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static void HandlebarBlockEachHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            JToken replacementObject = expression.GetCurrentJToken(expression.Expression, context);
            if (expression.IsTokenTruthy(replacementObject))
            {
                if (replacementObject.Type == JTokenType.Array)
                {
                    MustacheParsingUtil.EvaluateJToken(writer, replacementObject as JArray, context, expression);
                }
                else if (replacementObject.Type == JTokenType.Object)
                {
                    // Handle object/dictionaries
                    foreach (KeyValuePair<string, JToken> kvp in (IDictionary<string, JToken>)replacementObject)
                    {
                        MustacheEvaluationContext childContext = new MustacheEvaluationContext
                        {
                            ReplacementObject = kvp.Value,
                            ParentContext = context,
                            CurrentKey = kvp.Key,
                            PartialExpressions = context.PartialExpressions,
                            AdditionalEvaluationData = context.AdditionalEvaluationData,
                            Options = context.Options
                        };
                        MustacheEvaluationContext.CombinePartialsDictionaries(context, expression);
                        expression.EvaluateChildExpressions(childContext, writer);
                    }
                }
            }
        }

        /// <summary>
        /// {{#lookup ../foo @index}} block helper allows for indexing into an object by @index or @key
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static void HandlebarBlockLookupHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            if (!String.IsNullOrEmpty(expression.Expression))
            {
                string[] parts = expression.Expression.Split(' ');
                if (parts.Length == 2)
                {
                    String key = parts[1];
                    String selector = null;

                    if (String.Equals(key, "@index"))
                    {
                        key = context.CurrentIndex.ToString();
                        selector = String.Format("{0}[{1}]", parts[0], key);
                    }
                    else
                    {
                        if (String.Equals(key, "@key"))
                        {
                            key = context.CurrentKey;
                        }

                        if (!String.IsNullOrEmpty(key))
                        {
                            selector = String.Format("{0}.{1}", parts[0], key);
                        }
                    }

                    if (!String.IsNullOrEmpty(selector))
                    {
                        JToken replacementObject = expression.GetCurrentJToken(selector, context);
                        if (replacementObject != null && replacementObject.Type != JTokenType.Null)
                        {
                            writer.Write(replacementObject.ToString(), expression.Encode);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// {{> foo context }} helper looks for a partial template registered as 'foo' and evaluates it against 'context'
        /// Evaluates against the current context if 'context' is not given
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static void HandlebarPartialHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            String partialName = expression.GetRawHelperArgument(0);
            if (String.IsNullOrEmpty(partialName))
            {
                throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateInvalidPartialReference(expression.Expression));
            }

            // Dynamic partial syntax: lookup name of partial
            if (partialName[0].Equals('(') && partialName[partialName.Length - 1].Equals(')'))
            {
                JToken token = expression.GetCurrentJToken(partialName.Substring(1, partialName.Length - 2), context);
                if (token == null || !token.Type.Equals(JTokenType.String))
                {
                    return;
                }
                partialName = token.ToString();
            }

            MustacheRootExpression parentPartial;
            context.PartialExpressions.TryGetValue(partialName, out parentPartial);

            if (parentPartial != null)
            {
                // Evaluate partial with all partials registered within scope as well as within the partial template
                MustacheEvaluationContext.CombinePartialsDictionaries(context, parentPartial);

                // Get context
                MustacheEvaluationContext replacementContext = new MustacheEvaluationContext()
                {
                    ReplacementObject = context.ReplacementObject,
                    ParentContext = context.ParentContext,
                    PartialExpressions = context.PartialExpressions,
                    AdditionalEvaluationData = context.AdditionalEvaluationData,
                    Options = context.Options
                };

                String contextSelector = expression.GetRawHelperArgument(1);
                if (contextSelector != null)
                {
                    replacementContext.ReplacementObject = expression.GetCurrentJToken(contextSelector, context);
                    replacementContext.ParentContext = context;
                    replacementContext.PartialExpressions = context.PartialExpressions;
                }
                parentPartial.Evaluate(replacementContext, writer);
            }
        }
    }
}
