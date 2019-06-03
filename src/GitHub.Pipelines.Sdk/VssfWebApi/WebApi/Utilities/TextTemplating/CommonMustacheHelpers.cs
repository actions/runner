using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Static helper class for common mustache template helpers
    /// </summary>
    public static class CommonMustacheHelpers
    {
        public static Dictionary<String, MustacheTemplateHelperWriter> GetHelpers()
        {
            return new Dictionary<string, MustacheTemplateHelperWriter>(StringComparer.OrdinalIgnoreCase)
            {
                { "equals", EqualsHelper },
                { "notEquals", NotEqualsHelper },
                { "contains", StringContainsHelper },
                { "stringLeft", StringLeftHelper },
                { "stringRight", StringRightHelper },
                { "arrayLength", ArrayLengthHelper },
                { "date", DateHelper },
                { "stringFormat", StringFormatHelper },
                { "stringPadLeft", StringPadHelper },
                { "stringPadRight", StringPadHelper },
                { "stringReplace", StringReplaceHelper },
                { "stringLower", StringLowerHelper },
                { "and", LogicalAndHelper },
                { "or", LogicalOrHelper },

                // Left for compatibility
                { "stringContains", StringContainsHelper }
            };
        }

        public static void EqualsHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            String arg1 = expression.GetHelperArgument<String>(context, 0);
            String arg2 = expression.GetHelperArgument<String>(context, 1);
            Boolean ignoreCase = expression.GetHelperArgument(context, 2, false);

            if (String.Equals(arg1, arg2, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
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
            else if (!expression.IsBlockExpression)
            {
                writer.Write("false");
            }
        }

        public static void NotEqualsHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            String arg1 = expression.GetHelperArgument<String>(context, 0);
            String arg2 = expression.GetHelperArgument<String>(context, 1);
            Boolean ignoreCase = expression.GetHelperArgument(context, 2, false);

            if (String.Equals(arg1, arg2, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
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

        public static void StringContainsHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            String string1 = expression.GetHelperArgument(context, 0, String.Empty);
            String value = expression.GetHelperArgument(context, 1, String.Empty);

            if (string1 != null && value != null)
            {
                StringComparison comparer = expression.GetHelperArgument(context, 2, false) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (string1.IndexOf(value, comparer) >= 0)
                {
                    if (expression.IsBlockExpression)
                    {
                        expression.EvaluateChildExpressions(context, writer);
                    }
                    else
                    {
                        writer.Write("true");
                    }

                    return;
                }
            }

            if (!expression.IsBlockExpression)
            {
                writer.Write("false");
            }
        }

        public static void StringLeftHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            if (expression != null && context != null)
            {
                string inputString = expression.GetHelperArgument<string>(context, 0);
                int stringLength = expression.GetHelperArgument<int>(context, 1);

                if (stringLength > 0)
                {
                    string outputString = inputString;

                    if (inputString.Length > stringLength)
                    {
                        string truncatedMarker = expression.GetHelperArgument<string>(context, 2);
                        outputString = String.Concat(inputString.Substring(0, stringLength), truncatedMarker ?? string.Empty);
                    }

                    writer.Write(outputString, expression.Encode);
                }
            }
        }

        public static void StringRightHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            if (expression != null && context != null)
            {
                string inputString = expression.GetHelperArgument<string>(context, 0);
                int stringLength = expression.GetHelperArgument<int>(context, 1);

                if (stringLength > 0)
                {
                    string outputString = inputString;

                    if (inputString.Length > stringLength)
                    {
                        outputString = inputString.Substring(inputString.Length - stringLength, stringLength);
                    }

                    writer.Write(outputString, expression.Encode);
                }
            }
        }

        public static void ArrayLengthHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            try
            {
                if (expression != null && context != null)
                {
                    string selector = expression.GetRawHelperArgument(0);
                    if (!string.IsNullOrEmpty(selector))
                    {
                        JToken arrayJson = expression.GetCurrentJToken(selector, context);

                        if (arrayJson != null)
                        {
                            writer.Write(arrayJson.Children().Count().ToString());
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {
                //don't have context to log the exception, silently fail
            }

            writer.Write("0");
        }

        public static void DateHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            try
            {
                string dateTimeString = expression.GetHelperArgument<string>(context, 0);
                if (!string.IsNullOrEmpty(dateTimeString))
                {
                    DateTime dateTime = DateTime.Parse(dateTimeString);
                    writer.Write(dateTime.ToString("d"));
                }
            }
            catch (Exception)
            {
                //don't have context to log the exception, silently fail
            }
        }

        public static void StringPadHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            string inputString;
            int desiredStringLength;
            char padChar;

            if (expression.IsBlockExpression)
            {
                inputString = expression.EvaluateChildExpressions(context);
                desiredStringLength = expression.GetHelperArgument<int>(context, 0);
                padChar = expression.GetHelperArgument<string>(context, 1, " ")[0];
            }
            else
            {
                inputString = expression.GetHelperArgument<string>(context, 0);
                desiredStringLength = expression.GetHelperArgument<int>(context, 1);
                padChar = expression.GetHelperArgument<string>(context, 2, " ")[0];
            }

            // Both the left and right pad helpers are registered to the same function, so check the helper name
            if (String.Equals(expression.HelperName, "stringPadLeft", StringComparison.OrdinalIgnoreCase))
            {
                writer.Write(inputString.PadLeft(desiredStringLength, padChar));
            }
            else
            {
                writer.Write(inputString.PadRight(desiredStringLength, padChar));
            }
        }

        public static void StringReplaceHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            string inputString;
            string oldValue;
            string newValue;

            if (expression.IsBlockExpression)
            {
                inputString = expression.EvaluateChildExpressions(context);
                oldValue = expression.GetHelperArgument<string>(context, 0);
                newValue = expression.GetHelperArgument<string>(context, 1);
            }
            else
            {
                inputString = expression.GetHelperArgument<string>(context, 0);
                oldValue = expression.GetHelperArgument<string>(context, 1);
                newValue = expression.GetHelperArgument<string>(context, 2);
            }

            // inputString is the string we want to do replacement in. In case it is null or empty, skip the string Replace call.
            if (!String.IsNullOrWhiteSpace(inputString))
            {
                writer.Write(inputString.Replace(oldValue, newValue));
            }
            else
            {
                writer.Write("");
            }
        }

        public static void StringLowerHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            if (expression != null && context != null)
            {
                string inputString = expression.GetHelperArgument<string>(context, 0);
                writer.Write(inputString.ToLower());
            }
        }

        public static void StringFormatHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            try
            {
                if (expression != null && context != null)
                {
                    int numArguments = expression.HelperArguments.Count;

                    if (numArguments > 0)
                    {
                        string fmtString = expression.GetHelperArgument<string>(context, 0);

                        if (numArguments == 1)
                        {
                            writer.Write(fmtString, expression.Encode);
                        }
                        else
                        {
                            string[] parameters = new string[numArguments - 1];
                            for (int helperIndex = 1; helperIndex < numArguments; ++helperIndex)
                            {
                                parameters[helperIndex - 1] = expression.GetHelperArgument<string>(context, helperIndex);
                            }

                            string output = String.Format(fmtString, parameters);
                            writer.Write(output, expression.Encode);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //don't have context to log the exception, silently fail
            }
        }

        /// <summary>
        /// Helper that evaluates whether all of its arguments are "truthy" (e.g. is a non-zero numeric value, non-empty string, non-empty array, or non-empty object)
        ///
        /// Usage:
        ///   {{#and arg1 arg2}}Yes, both arguments are truthy{{/and}}
        ///
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public static void LogicalAndHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            WriteTruthyOutput(expression, writer, context, expression.HelperArguments != null && expression.HelperArguments.Count > 0 && expression.HelperArguments.All(a => MustacheTemplatedExpression.IsHelperArgumentTruthy(context, a)));
        }

        /// <summary>
        /// Helper that evaluates whether at least 1 of its arguments is "truthy" (e.g. is a non-zero numeric value, non-empty string, non-empty array, or non-empty object)
        ///
        /// Usage:
        ///   {{#or arg1 arg2}}Yes, at least one of the arguments is truthy{{/or}}
        ///
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public static void LogicalOrHelper(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context)
        {
            WriteTruthyOutput(expression, writer, context, expression.HelperArguments != null && expression.HelperArguments.Count > 0 && expression.HelperArguments.Any(a => MustacheTemplatedExpression.IsHelperArgumentTruthy(context, a)));
        }

        private static void WriteTruthyOutput(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context, Boolean isTruthy)
        {
            if (isTruthy)
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
            else if (!expression.IsBlockExpression)
            {
                writer.Write("false");
            }
        }
    }
}
