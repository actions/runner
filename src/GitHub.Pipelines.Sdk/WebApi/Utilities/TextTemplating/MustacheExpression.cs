using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Common.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Base exception for mustache parse and evaluation exceptions
    /// </summary>
    public class MustacheException : VssServiceException
    {
        public MustacheException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when parsing a mustache template that indicates it is an invalid expression
    /// </summary>
    [ExceptionMapping("0.0", "3.0", "MustacheExpressionInvalidException", "Microsoft.VisualStudio.Services.WebApi.MustacheExpressionInvalidException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class MustacheExpressionInvalidException : MustacheException
    {
        public MustacheExpressionInvalidException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a mustache evaluation result exceeds the maximum length
    /// </summary>
    public class MustacheEvaluationResultLengthException : MustacheException
    {
        public MustacheEvaluationResultLengthException(String message)
            : base(message)
        {
        }
    }

    [Obsolete("Use MustacheTemplateHelperMethod")]
    public delegate String MustacheTemplateHelper(MustacheTemplatedExpression expression, MustacheEvaluationContext context);

    /// <summary>
    /// Delegate for methods called during evaluation for registered helpers
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public delegate Object MustacheTemplateHelperMethod(MustacheTemplatedExpression expression, MustacheEvaluationContext context);

    /// <summary>
    /// Delegate for methods called during evaluation for registered helpers
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public delegate void MustacheTemplateHelperWriter(MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context);

    /// <summary>
    /// Delegate for encoding values during evaluation.
    /// </summary>
    /// <param name="value">value to be encoded</param>
    /// <returns>the encoded value</returns>
    public delegate String MustacheEncodeMethod(String value);

    /// <summary>
    /// Context object used during the evaulation of a mustache template expression
    /// </summary>
    public class MustacheEvaluationContext
    {
        private JToken m_replacementToken;
        private IDictionary<MustacheTemplatedExpression, Boolean> m_evaluatedExpressionTruthiness;
        private MustacheOptions m_options;

        /// <summary>
        /// The replacement object used to evaluate expressions in this context
        /// </summary>
        public Object ReplacementObject { get; set; }

        public Dictionary<String, Object> AdditionalEvaluationData { get; set; }

        /// <summary>
        /// The replacement object used to evaluate expressions in this context
        /// </summary>
        internal JToken ReplacementToken
        {
            get
            {
                if (m_replacementToken == null && ReplacementObject != null)
                {
                    m_replacementToken = ReplacementObject as JToken;
                    if (m_replacementToken == null)
                    {
                        m_replacementToken = JToken.FromObject(ReplacementObject);
                    }
                }
                return m_replacementToken;
            }
        }

        /// <summary>
        /// The context object for the parent expression
        /// </summary>
        public MustacheEvaluationContext ParentContext { get; set; }

        /// <summary>
        /// The current index of the parent expression's array (only applicable when the parent context is an array)
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// The total number of items in the parent context (only applicable when the parent context is an array)
        /// </summary>
        public int ParentItemsCount { get; set; }

        /// <summary>
        /// The current key of the parent expression's object (only applicable when the parent context is an each/object expression)
        /// </summary>
        public String CurrentKey { get; set; }

        /// <summary>
        /// The complete set of partial accessible from this context
        /// </summary>
        public Dictionary<string, MustacheRootExpression> PartialExpressions { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public MustacheOptions Options
        {
            get
            {
                if (m_options == null)
                {
                    m_options = new MustacheOptions();
                }

                return m_options;
            }

            set
            {
                m_options = value;
            }
        }

        // Creates a new dictionary with elements of dict2 to dict1, if the key doesn't already exist in dict1
        public static void CombinePartialsDictionaries(MustacheEvaluationContext context, MustacheAggregateExpression expression)
        {
            Dictionary<String, MustacheRootExpression> combinedDict = new Dictionary<string, MustacheRootExpression>();
            foreach (String key in context.PartialExpressions.Keys)
            {
                combinedDict.TryAdd(key, context.PartialExpressions[key]);
            }
            foreach (String key in expression.PartialExpressions.Keys)
            {
                combinedDict.TryAdd(key, expression.PartialExpressions[key]);
            }
            context.PartialExpressions = combinedDict;
        }

        internal void AssertCancellation()
        {
            Options.CancellationToken.ThrowIfCancellationRequested();
        }

        internal Boolean? WasExpressionEvaluatedAsTruthy(MustacheTemplatedExpression expression)
        {
            Boolean truthy;
            if (m_evaluatedExpressionTruthiness != null && m_evaluatedExpressionTruthiness.TryGetValue(expression, out truthy))
            {
                return truthy;
            }
            else
            {
                return null;
            }
        }

        internal void StoreExpressionTruthiness(MustacheTemplatedExpression expression, Boolean truthy)
        {
            if (m_evaluatedExpressionTruthiness == null)
            {
                m_evaluatedExpressionTruthiness = new Dictionary<MustacheTemplatedExpression, Boolean>();
            }
            m_evaluatedExpressionTruthiness[expression] = truthy;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class MustacheOptions
    {
        private MustacheEncodeMethod m_encodeMethod;

        public MustacheOptions()
        {
            MaxDepth = Int32.MaxValue;
            MaxResultLength = Int32.MaxValue;
        }

        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicationg whether inline partials are disabled. When true, an exception will be thrown
        /// by the parser if an inline partial is defined.
        /// </summary>
        public Boolean DisableInlinePartials { get; set; }

        /// <summary>
        /// Gets or sets the delegate for encoding values during evaluation. Defaults to the method <see cref="MustacheEncodeMethods.HtmlEncode(string)"/>.
        /// </summary>
        public MustacheEncodeMethod EncodeMethod
        {
            get
            {
                return m_encodeMethod ?? MustacheEncodeMethods.HtmlEncode;
            }

            set
            {
                m_encodeMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth. This number limits the maximum nest level at parse time. In the future,
        /// this setting will be applied at evaluation time as well. Until then, inline partials should also be disabled
        /// in order for a max depth to be enforced. Any number less than 1 is treated as Int32.MaxValue. An exception
        /// will be thrown when the threshold is exceeded.
        /// </summary>
        public Int32 MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum string length for the evaluation result. Any number less than 1 is treated
        /// as Int32.MaxValue. Evaluation will throw an exception when the threshold is exceeded.
        /// </summary>
        public Int32 MaxResultLength { get; set; }
    }

    public sealed class MustacheTextWriter : TextWriter
    {
        private TextWriter m_writer;
        private MustacheOptions m_options;
        private Int32 m_resultLength;

        public Int32 ResultLength
        {
            get
            {
                return m_resultLength;
            }
        }

        public override Encoding Encoding
        {
            get
            {
                return m_writer.Encoding;
            }
        }

        public MustacheTextWriter(TextWriter writer, MustacheOptions options)
        {
            m_writer = writer;
            m_options = options;
        }

        public override void Write(char value)
        {
            if (++m_resultLength > m_options.MaxResultLength)
            {
                throw new MustacheEvaluationResultLengthException(WebApiResources.MustacheEvaluationResultLengthExceeded(m_options.MaxResultLength));
            }

            m_writer.Write(value);
        }

        public void Write(String value, Boolean encode)
        {
            if (encode)
            {
                value = m_options.EncodeMethod(value);
            }

            Write(value);
        }

        public override void Flush()
        {
            m_writer.Flush();
        }

        public override Task FlushAsync()
        {
            return m_writer.FlushAsync();
        }
    }

    /// <summary>
    /// Base class for mustache template expressions
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class MustacheExpression
    {
        /// <summary>
        /// Does this type of expression accept a context/replacement object during evaluation
        /// </summary>
        public abstract bool IsContextBased { get; }

        /// <summary>
        /// The complete set of partial accessible from this context
        /// </summary>
        public Dictionary<string, MustacheRootExpression> PartialExpressions { get; set; }

        /// <summary>
        /// Method called during the evaluation of an expression
        /// </summary>
        /// <param name="context"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        internal abstract void Evaluate(MustacheEvaluationContext context, MustacheTextWriter writer);

        /// <summary>
        /// Evaluate the current expression using the given replacement object as context.
        /// </summary>
        /// <param name="replacementObject"></param>
        /// <returns></returns>
        public String Evaluate(
            Object replacementObject,
            Dictionary<String, Object> additionalEvaluationData = null,
            MustacheEvaluationContext parentContext = null,
            Dictionary<String, MustacheRootExpression> partialExpressions = null)
        {
            return this.Evaluate(
                replacementObject: replacementObject,
                additionalEvaluationData: additionalEvaluationData,
                parentContext: parentContext,
                partialExpressions: partialExpressions,
                options: null);
        }

        /// <summary>
        /// Evaluate the current expression using the given replacement object as context.
        /// </summary>
        /// <param name="replacementObject"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public String Evaluate(
            Object replacementObject,
            Dictionary<String, Object> additionalEvaluationData,
            MustacheEvaluationContext parentContext,
            Dictionary<String, MustacheRootExpression> partialExpressions,
            MustacheOptions options)
        {
            using (StringWriter stringWriter = new StringWriter())
            {
                Evaluate(stringWriter, replacementObject, options, additionalEvaluationData, parentContext, partialExpressions);
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Evaluate the current expression using the given replacement object as context.
        /// </summary>
        /// <param name="replacementObject"></param>
        /// <returns></returns>
        public void Evaluate(
            TextWriter writer,
            Object replacementObject,
            MustacheOptions options = null,
            Dictionary<String, Object> additionalEvaluationData = null,
            MustacheEvaluationContext parentContext = null,
            Dictionary<String, MustacheRootExpression> partialExpressions = null)
        {
            if (options == null)
            {
                options = new MustacheOptions();
            }

            MustacheTextWriter mustacheWriter = new MustacheTextWriter(writer, options);

            MustacheEvaluationContext evaluationContext = new MustacheEvaluationContext
            {
                ReplacementObject = replacementObject,
                PartialExpressions = PartialExpressions,
                AdditionalEvaluationData = additionalEvaluationData,
                ParentContext = parentContext,
                Options = options
            };

            // Apply the options to all ancestor contexts.
            MustacheEvaluationContext ancestorContext = parentContext;
            while (ancestorContext != null)
            {
                ancestorContext.Options = options;
                ancestorContext = ancestorContext.ParentContext;
            }

            if (partialExpressions != null)
            {
                evaluationContext.PartialExpressions = partialExpressions;
            }

            evaluationContext.AssertCancellation();
            this.Evaluate(evaluationContext, mustacheWriter);
        }

        /// <summary>
        /// Parses a mustache-template expression
        /// </summary>
        /// <param name="template">mustache-style template</param>
        /// <param name="helpers">mustache template helper functions</param>
        /// <param name="blockHelpers">mustache block template helper functions</param>
        /// <param name="options">mustache options for parsing and evaluation</param>
        /// <param name="depth">current depth of the caller</param>
        /// <returns></returns>
        internal static MustacheRootExpression Parse(
            String template,
            Dictionary<String, MustacheTemplateHelperWriter> helpers,
            Dictionary<String, MustacheRootExpression> partials,
            MustacheOptions options,
            Int32 depth = 0)
        {
            if (options == null)
            {
                options = new MustacheOptions();
            }

            // Don't increment the depth for the root expression
            MustacheRootExpression rootExpression = new MustacheRootExpression()
            {
                TemplateHelpers = helpers,
                PartialExpressions = partials == null ? new Dictionary<string, MustacheRootExpression>(StringComparer.OrdinalIgnoreCase) : partials
            };

            MustacheAggregateExpression currentExpression = rootExpression;

            StringBuilder sbExpression = new StringBuilder();
            bool expressionIsTemplate = false;
            bool expressionIsEncodedTemplate = false;
            bool expressionIsBlockComment = false;

            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];

                if (c == '\\' && MustacheParsingUtil.SafeCharAt(template, i + 1) == '{' && MustacheParsingUtil.SafeCharAt(template, i + 2) == '{' && !expressionIsBlockComment)
                {
                    // A sequence of '\{{' is an escape sequence for a literal {{
                    sbExpression.Append("{{");
                    i += 2;
                }
                else if (c == '\\' && MustacheParsingUtil.SafeCharAt(template, i + 1) == '}' && MustacheParsingUtil.SafeCharAt(template, i + 2) == '}' && !expressionIsBlockComment)
                {
                    // A sequence of '\}}' is an escape sequence for a literal }}
                    sbExpression.Append("}}");
                    i += 2;
                }
                else if (c == '{' && MustacheParsingUtil.SafeCharAt(template, i + 1) == '{' && !expressionIsBlockComment &&
                    MustacheParsingUtil.SafeCharAt(template, i - 1) != '\\')
                {
                    if (expressionIsTemplate)
                    {
                        // An unescaped sequence of '{{' is not legal inside an expression template. E.g. {{nested {{ is not legal}}
                        throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateInvalidStartBraces(sbExpression.ToString(), i, template));
                    }

                    // Start braces

                    if (sbExpression.Length > 0)
                    {
                        // Flush the text buffer
                        MustacheParsingUtil.AssertDepth(options, depth + 1);
                        AddTextExpression(currentExpression, sbExpression.ToString(), false);
                    }

                    expressionIsTemplate = true;
                    sbExpression = new StringBuilder();

                    // Determine is encoded
                    // Adjust the index
                    if (MustacheParsingUtil.SafeCharAt(template, i + 2) == '{')
                    {
                        expressionIsEncodedTemplate = false;
                        i += 2;
                    }
                    else
                    {
                        expressionIsEncodedTemplate = true;
                        i += 1;
                    }

                    // Determine is comment
                    if (MustacheParsingUtil.SafeCharAt(template, i + 1) == '!' && MustacheParsingUtil.SafeCharAt(template, i + 2) == '-' && MustacheParsingUtil.SafeCharAt(template, i + 3) == '-')
                    {
                        expressionIsBlockComment = true;
                    }
                    else
                    {
                        expressionIsBlockComment = false;
                    }
                }
                else if (c == '}' && MustacheParsingUtil.SafeCharAt(template, i + 1) == '}' && expressionIsTemplate &&
                        (!expressionIsBlockComment || (MustacheParsingUtil.SafeCharAt(template, i - 1) == '-' && MustacheParsingUtil.SafeCharAt(template, i - 2) == '-')))
                {
                    // Validate the number of closing braces
                    // Adjust the index
                    if (expressionIsEncodedTemplate)
                    {
                        i += 1;
                    }
                    else
                    {
                        if (MustacheParsingUtil.SafeCharAt(template, i + 2) != '}')
                        {
                            // An unecoded template expression must be closed using three braces. E.g. {{{two closing braces is not legal here}}
                            throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateBraceCountMismatch(sbExpression.ToString()));
                        }
                        i += 2;
                    }

                    String expressionText = sbExpression.ToString();
                    if (expressionText.StartsWith("/"))
                    {
                        // End-block expression 
                        String endBlockName = expressionText.Substring(1);
                        MustacheTemplatedExpression parentTemplateExpression = currentExpression as MustacheTemplatedExpression;
                        if (parentTemplateExpression == null || !parentTemplateExpression.IsBlockExpression)
                        {
                            // An end-block expression must have a parent expression
                            throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateInvalidEndBlock(
                                expressionText));
                        }
                        else if (!String.Equals(endBlockName, parentTemplateExpression.Expression, StringComparison.OrdinalIgnoreCase) &&
                            !String.Equals(endBlockName, parentTemplateExpression.HelperName, StringComparison.OrdinalIgnoreCase))
                        {
                            // An end-block expression must match it's parent either by full expression text or by helper-name
                            throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateNonMatchingEndBlock(
                                expressionText,
                                parentTemplateExpression.Expression));
                        }
                        else
                        {
                            // Pop the parent expression
                            currentExpression = currentExpression.ParentExpression;
                            depth--;
                        }
                    }
                    else
                    {
                        bool isBlockExpression = false;
                        bool isInvertedExpression = false;
                        MustacheTemplatedExpression elseSourceExpression = null;

                        // inline partials
                        if (expressionText.StartsWith("#*inline"))
                        {
                            if (options.DisableInlinePartials)
                            {
                                // Inline partials are disabled
                                throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateInlinePartialsNotAllowed());
                            }

                            // Get name of inline partial: e.g. #*inline "myPartial" => partialName = myPartial
                            var partialName = expressionText.Substring("#*inline".Length).Trim(new char[] { ' ', '"', '\'' });
                            // Find the end of the inline
                            var endIndex = template.IndexOf("{{/inline}}", i);
                            // Inline isn't closed
                            if (endIndex == -1)
                            {
                                throw new MustacheExpressionInvalidException(WebApiResources.MissingCloseInlineMessage());
                            }
                            // Inline contains another inline
                            else if (template.Substring(i, endIndex - i).Contains("#*inline"))
                            {
                                throw new MustacheExpressionInvalidException(WebApiResources.NestedInlinePartialsMessage());
                            }

                            // Count the partial declaration as contributing to the parse depth
                            MustacheParsingUtil.AssertDepth(options, depth + 1);

                            // Create partial expression
                            var partialTemplate = template.Substring(i + 1, endIndex - (i + 1));
                            MustacheRootExpression partialExpression = MustacheExpression.Parse(
                                partialTemplate,
                                helpers,
                                partials: null,
                                options: options,
                                depth: depth + 1);
                            currentExpression.PartialExpressions.Add(partialName, partialExpression);
                            i = endIndex + "{{/inline}}".Length - 1;
                        }
                        else
                        {
                            if (expressionText.StartsWith("#"))
                            {
                                isBlockExpression = true;
                                expressionText = expressionText.Substring(1);
                            }
                            else if (expressionText.StartsWith("^"))
                            {
                                isBlockExpression = true;
                                isInvertedExpression = true;
                                expressionText = expressionText.Substring(1);
                            }
                            else if (String.Equals(expressionText, "else", StringComparison.OrdinalIgnoreCase) &&
                                currentExpression is MustacheTemplatedExpression)
                            {
                                elseSourceExpression = (MustacheTemplatedExpression)currentExpression;
                                isBlockExpression = true;
                                isInvertedExpression = !elseSourceExpression.IsNegativeExpression;
                                expressionIsEncodedTemplate = elseSourceExpression.Encode;

                                if (!String.IsNullOrEmpty(elseSourceExpression.HelperName))
                                {
                                    expressionText = elseSourceExpression.HelperName + " " + elseSourceExpression.Expression;
                                }
                                else
                                {
                                    expressionText = elseSourceExpression.Expression;
                                }

                                // Pop the parent expression since else is a sibling
                                currentExpression = currentExpression.ParentExpression;
                                depth--;
                            }

                            // Create the child expression
                            MustacheTemplatedExpression templateExpression = new MustacheTemplatedExpression(
                                expressionText.Trim(),
                                currentExpression,
                                rootExpression,
                                isBlockExpression,
                                isInvertedExpression,
                                elseSourceExpression != null,
                                expressionIsEncodedTemplate);

                            if (elseSourceExpression != null)
                            {
                                templateExpression.ElseSourceExpression = elseSourceExpression;
                            }

                            // Add the child expression
                            MustacheParsingUtil.AssertDepth(options, depth + 1);
                            currentExpression.ChildExpressions.Add(templateExpression);

                            if (isBlockExpression)
                            {
                                // Push the expression since it marks the beginning of a nested block
                                currentExpression = templateExpression;
                                depth++;
                            }
                        }
                    }

                    expressionIsTemplate = false;
                    expressionIsBlockComment = false;
                    expressionIsEncodedTemplate = false;
                    sbExpression = new StringBuilder();
                }
                else
                {
                    sbExpression.Append(c);
                }
            }

            if (sbExpression.Length > 0 && expressionIsTemplate)
            {
                throw new MustacheExpressionInvalidException(WebApiResources.MissingEndingBracesMessage(sbExpression.ToString()));
            }

            if (sbExpression.Length > 0)
            {
                // Assert additional depth is OK.
                MustacheParsingUtil.AssertDepth(options, depth + 1);
            }

            // Call AddTextExpression with isLastEntry=true, even if the buffer is empty.
            // The method trims trailing spaces from the previous expression in certain cases.
            AddTextExpression(currentExpression, sbExpression.ToString(), isLastEntry: true);

            return rootExpression;
        }

        private static void AddTextExpression(MustacheAggregateExpression expression, String text, Boolean isLastEntry)
        {
            // Handle block expressions which have their start and/or end block alone on a given line of text
            // in that case, the enclosed whitespace and newline character will be stripped-out.

            MustacheTextExpression previousTextExpression = null;
            Boolean isFirstEntry = false;

            MustacheTemplatedExpression templatedExpression = expression as MustacheTemplatedExpression;
            if (templatedExpression != null &&
                templatedExpression.IsBlockExpression &&
                templatedExpression.ChildExpressions.Count == 0)
            {
                // This is the first text expression inside a block expression
                // Get the text expression preceeding the block.
                if (templatedExpression.ElseSourceExpression != null)
                {
                    previousTextExpression = templatedExpression.ElseSourceExpression.ChildExpressions.LastOrDefault() as MustacheTextExpression;
                }
                else
                {
                    if (templatedExpression.ParentExpression.ChildExpressions.Count > 1)
                    {
                        previousTextExpression = templatedExpression.ParentExpression.ChildExpressions[templatedExpression.ParentExpression.ChildExpressions.Count - 2] as MustacheTextExpression;
                    }

                    if (templatedExpression.ParentExpression is MustacheRootExpression)
                    {
                        isFirstEntry = templatedExpression.ParentExpression.ChildExpressions.Count == 1 ||
                            (templatedExpression.ParentExpression.ChildExpressions.Count == 2 && previousTextExpression != null);
                    }
                }
            }
            else if (expression.ChildExpressions.Count > 0)
            {
                MustacheTemplatedExpression previousBlockExpression = expression.ChildExpressions[expression.ChildExpressions.Count - 1] as MustacheTemplatedExpression;
                if (previousBlockExpression != null &&
                    previousBlockExpression.IsBlockExpression &&
                    previousBlockExpression.ChildExpressions.Count > 0)
                {
                    // This is the first text expression after a block expression. Get the last text expression within the block.
                    previousTextExpression = previousBlockExpression.ChildExpressions[previousBlockExpression.ChildExpressions.Count - 1] as MustacheTextExpression;
                }
            }

            if (isFirstEntry || previousTextExpression != null)
            {
                // We are in the text-block-text scenario. Do our whitespace trimming if the block is alone on its line.
                int lastNewline = -1;
                if (previousTextExpression != null)
                {
                    lastNewline = previousTextExpression.Text.LastIndexOf('\n');
                }
                if (previousTextExpression == null ||
                    (isFirstEntry && lastNewline < 0 && String.IsNullOrWhiteSpace(previousTextExpression.Text)) ||
                    (lastNewline >= 0 && String.IsNullOrWhiteSpace(previousTextExpression.Text.Substring(lastNewline + 1))))
                {
                    int firstNewline = text.IndexOf('\n');
                    if (firstNewline >= 0 && String.IsNullOrWhiteSpace(text.Substring(0, firstNewline)) ||
                        (isLastEntry && String.IsNullOrWhiteSpace(text)))
                    {
                        if (firstNewline >= 0)
                        {
                            text = text.Substring(firstNewline + 1);
                        }
                        else
                        {
                            text = String.Empty;
                        }

                        if (previousTextExpression != null)
                        {
                            if (lastNewline < 0)
                            {
                                previousTextExpression.Text = String.Empty;
                            }
                            else
                            {
                                previousTextExpression.Text = previousTextExpression.Text.Substring(0, lastNewline + 1);
                            }
                        }
                    }
                }
            }

            if (text.Length > 0)
            {
                expression.ChildExpressions.Add(new MustacheTextExpression(text));
            }
        }
    }

    /// <summary>
    /// A text-only mustache expression (no {{ ... }} blocks)
    /// </summary>
    public class MustacheTextExpression : MustacheExpression
    {
        /// <summary>
        /// The raw text for this expression
        /// </summary>
        public String Text { get; set; }

        public override bool IsContextBased
        {
            get
            {
                return false;
            }
        }

        internal MustacheTextExpression(String text)
        {
            Text = text;
        }

        /// <summary>
        /// Get the text defined in this expression
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override void Evaluate(MustacheEvaluationContext context, MustacheTextWriter writer)
        {
            writer.Write(Text);
        }
    }

    /// <summary>
    /// A literal expression (i.e. string, boolean, number, true/false, null). Represents literal arguments for a helper expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Obsolete expression type")]
    public class MustacheLiteralExpression<T> : MustacheExpression
    {
        /// <summary>
        /// Literal value
        /// </summary>
        public T Value { get; }

        public override bool IsContextBased
        {
            get
            {
                return false;
            }
        }

        public MustacheLiteralExpression(T value)
        {
            this.Value = value;
        }

        internal override void Evaluate(MustacheEvaluationContext context, MustacheTextWriter writer)
        {
            if (Value != null)
            {
                writer.Write(Value.ToString());
            }
        }
    }

    /// <summary>
    /// A mustache expression which may contain child expressions
    /// </summary>
    public class MustacheRootExpression : MustacheAggregateExpression
    {
        /// <summary>
        /// Registered helper methods for single (non-block) expressions
        /// </summary>
        [Obsolete]
        public Dictionary<String, MustacheTemplateHelper> Helpers { get; set; }

        /// <summary>
        /// Registered helper methods for Block expressions
        /// </summary>
        [Obsolete]
        public Dictionary<String, MustacheTemplateHelper> BlockHelpers { get; set; }

        /// <summary>
        /// Template helpers
        /// </summary>
        internal Dictionary<String, MustacheTemplateHelperWriter> TemplateHelpers { get; set; }
    }

    /// <summary>
    /// A mustache expression which may contain child expressions
    /// </summary>
    public abstract class MustacheAggregateExpression : MustacheExpression
    {
        /// <summary>
        /// The parent expression which contains this expression
        /// </summary>
        internal MustacheAggregateExpression ParentExpression { get; set; }

        /// <summary>
        /// Expressions contained within this expression
        /// </summary>
        internal List<MustacheExpression> ChildExpressions { get; set; }

        internal MustacheAggregateExpression()
        {
            ChildExpressions = new List<MustacheExpression>();
            PartialExpressions = new Dictionary<string, MustacheRootExpression>();
        }

        public override bool IsContextBased
        {
            get
            {
                return ChildExpressions.Any(child =>
                {
                    return child is MustacheTemplatedExpression;
                });
            }
        }

        /// <summary>
        /// Get the resolved value from this and all child expressions
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override void Evaluate(MustacheEvaluationContext context, MustacheTextWriter writer)
        {
            EvaluateChildExpressions(context, writer);
        }

        /// <summary>
        /// Evaluate all child expressions with the given context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public String EvaluateChildExpressions(MustacheEvaluationContext context)
        {
            using (StringWriter writer = new StringWriter())
            {
                MustacheTextWriter mustacheWriter = new MustacheTextWriter(writer, context.Options);

                foreach (MustacheExpression expression in ChildExpressions)
                {
                    context.AssertCancellation();
                    expression.Evaluate(context, mustacheWriter);
                }

                return writer.ToString();
            }
        }

        /// <summary>
        /// Evaluate all child expressions with the given context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void EvaluateChildExpressions(MustacheEvaluationContext context, MustacheTextWriter writer)
        {
            foreach (MustacheExpression expression in ChildExpressions)
            {
                context.AssertCancellation();
                expression.Evaluate(context, writer);
            }
        }

        /// <summary>
        /// Get the current token/replacement object for the given selector and context
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public JToken GetCurrentJToken(String selector, MustacheEvaluationContext context)
        {
            if (String.IsNullOrEmpty(selector) || context.ReplacementObject == null)
            {
                return null;
            }

            JToken currentToken = context.ReplacementToken;

            if (selector.StartsWith("@root"))
            {
                bool isRootSelector = false;

                if (selector.Length == "@root".Length)
                {
                    isRootSelector = true;
                    selector = null;
                }
                else if ((selector["@root".Length] == '.' || selector["@root".Length] == '/'))
                {
                    isRootSelector = true;
                    selector = selector.Substring("@root".Length + 1);
                }

                if (isRootSelector)
                {
                    while (context.ParentContext != null)
                    {
                        context = context.ParentContext;
                        currentToken = context.ReplacementToken;
                    }
                }
            }
            else
            {
                while (selector.StartsWith("..") && context.ParentContext != null)
                {
                    context = context.ParentContext;
                    currentToken = context.ReplacementToken;
                    selector = selector.Substring(2).TrimStart('/');
                }

                if (String.Equals(selector, ".") || String.Equals(selector, "this"))
                {
                    selector = null;
                }
                else if (String.Equals(selector, "@index"))
                {
                    return context.CurrentIndex.ToString();
                }
                else if (String.Equals(selector, "@key"))
                {
                    return context.CurrentKey;
                }
                else if (String.Equals(selector, "@first"))
                {
                    return context.CurrentIndex == 0;
                }
                else if (String.Equals(selector, "@last"))
                {
                    return context.CurrentIndex == context.ParentItemsCount - 1;
                }
                else if (selector.StartsWith("./"))
                {
                    selector = selector.Substring("./".Length);
                }
                else if (selector.StartsWith("this"))
                {
                    if (selector.StartsWith("this/"))
                    {
                        selector = selector.Substring("this/".Length);
                    }
                    else if (selector.StartsWith("this."))
                    {
                        selector = selector.Substring("this.".Length);
                    }
                }
            }

            // Select the new token for this expression
            if (!String.IsNullOrEmpty(selector) && currentToken != null)
            {
                try
                {
                    currentToken = currentToken.SelectToken(selector);
                }
                catch (JsonException)
                {
                    currentToken = null;
                }
            }

            return currentToken;
        }
    }

    /// <summary>
    /// A templated mustache expression ({{ ... }}). May be a block or simple expression.
    /// </summary>
    public class MustacheTemplatedExpression : MustacheAggregateExpression
    {
        private IList<Object> m_helperArguments;

        /// <summary>
        /// The root expression
        /// </summary>
        internal MustacheRootExpression RootExpression { get; set; }

        /// <summary>
        /// The selector/expression context for this block
        /// </summary>
        public String Expression { get; protected set; }

        /// <summary>
        /// The name of the helper method to invoke (if any)
        /// </summary>
        public String HelperName { get; set; }

        /// <summary>
        /// Argument expressions for this expression (if any)
        /// </summary>
        public IList<Object> HelperArguments
        {
            get
            {
                if (m_helperArguments == null)
                {
                    m_helperArguments = ParseArgumentExpressions();
                }
                return m_helperArguments;
            }
        }

        /// <summary>
        /// True to encode the result
        /// </summary>
        public Boolean Encode { get; set; }

        /// <summary>
        /// Is the expression a block expression ({{#...}})
        /// </summary>
        public Boolean IsBlockExpression { get; set; }

        /// <summary>
        /// Is the expression a negative expression ({{^...}} or {{else}})
        /// </summary>
        public Boolean IsNegativeExpression { get; set; }

        /// <summary>
        /// Is this an else block
        /// </summary>
        public Boolean IsElseBlock { get; set; }

        /// <summary>
        /// If this is an else block, this is the source (if) expression to compliment
        /// </summary>
        public MustacheTemplatedExpression ElseSourceExpression { get; set; }

        /// <summary>
        /// Is this a comment exception ({{!-- ... --}} or {{! ... }})
        /// </summary>
        public Boolean IsComment { get; set; }

        public MustacheTemplatedExpression(
            String expression,
            MustacheAggregateExpression parentExpression,
            MustacheRootExpression rootExpression,
            bool isBlockExpression = false,
            bool isInvertedExpression = false,
            bool isElseBlock = false,
            bool encode = false)
        {
            RootExpression = rootExpression;
            ParentExpression = parentExpression;
            Expression = expression;
            IsBlockExpression = isBlockExpression;
            IsNegativeExpression = isInvertedExpression;
            IsElseBlock = isElseBlock;
            Encode = encode;

            if (!String.IsNullOrEmpty(expression))
            {
                if (!IsBlockExpression && expression.StartsWith("!"))
                {
                    IsComment = true;
                }
                else
                {
                    int spaceIndex = expression.IndexOf(' ');
                    if (spaceIndex >= 0)
                    {
                        // Handle {{helper ...}
                        HelperName = expression.Substring(0, spaceIndex);
                        Expression = expression.Substring(spaceIndex + 1);

                        if (RootExpression.TemplateHelpers == null || !RootExpression.TemplateHelpers.ContainsKey(HelperName))
                        {
                            throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateMissingHelper(HelperName, expression));
                        }
                    }
                    else
                    {
                        // No space but this could still be a helper method
                        if (!expression.StartsWith(".") && !expression.StartsWith("this/") && !expression.StartsWith("this."))
                        {
                            if (RootExpression.TemplateHelpers != null && RootExpression.TemplateHelpers.ContainsKey(expression))
                            {
                                HelperName = expression;
                                Expression = String.Empty;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Templated expressions accept replacement context
        /// </summary>
        public override bool IsContextBased
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Get the current token/replacement object for the given selector and context
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Object GetCurrentToken(String selector, MustacheEvaluationContext context)
        {
            return GetCurrentJToken(selector, context);
        }

        /// <summary>
        /// Determine whether or not the token evaluates to a "truthy" value (non-null, non-empty string, non-zero, etc.)
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Boolean IsTokenTruthy(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }

            switch (token.Type)
            {
                case JTokenType.Array:
                    return token.AsJEnumerable().Any();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Float:
                case JTokenType.Integer:

                    return token.Value<double>() != 0;

                case JTokenType.String:

                    return !String.IsNullOrEmpty(token.Value<String>());

                case JTokenType.Object:
                    return token.Value<object>() != null;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Evaluate the value of this expression using the given context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override void Evaluate(MustacheEvaluationContext context, MustacheTextWriter writer)
        {
            Int32 originalLength = writer.ResultLength;
            EvaluateInternal(context, writer);
            context.StoreExpressionTruthiness(this, writer.ResultLength > originalLength);
        }

        /// <summary>
        /// Evaluate the value of this expression using the given context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private void EvaluateInternal(MustacheEvaluationContext context, MustacheTextWriter writer)
        {
            if (IsComment)
            {
                // Comment expression evaluates to empty string
                return;
            }

            if (ElseSourceExpression != null)
            {
                Boolean? ifExpressionTruthy = context.WasExpressionEvaluatedAsTruthy(ElseSourceExpression);
                if (ifExpressionTruthy == null)
                {
                    ifExpressionTruthy = !String.IsNullOrEmpty(ElseSourceExpression.Evaluate(context));
                }

                if (!ifExpressionTruthy.Value)
                {
                    base.Evaluate(context, writer);
                }

                return;
            }

            // Look for a registered helper - and let it handle evaluating this expression
            if (!String.IsNullOrEmpty(HelperName))
            {
                MustacheTemplateHelperWriter helper = null;

                if (RootExpression.TemplateHelpers != null)
                {
                    RootExpression.TemplateHelpers.TryGetValue(HelperName, out helper);
                }

                helper?.Invoke(this, writer, context);

                return;
            }

            // Evaluate the token
            JToken currentToken = GetCurrentJToken(Expression, context);
            Boolean isTruthy = IsTokenTruthy(currentToken);

            // Handle negative expressions
            if (IsNegativeExpression)
            {
                if (!isTruthy)
                {
                    // Negative expression and token is negative
                    base.Evaluate(context, writer);
                }

                return;
            }

            // Non-block expressions, just return token value
            if (!IsBlockExpression)
            {
                // A non-block expression - return the token's value
                if (currentToken != null && currentToken.Type != JTokenType.Null)
                {
                    String rawTokenString = currentToken.ToString();
                    writer.Write(rawTokenString, this.Encode);
                }

                return;
            }

            //
            // Block expressions
            //

            // Handle null token
            if (!isTruthy)
            {
                return;
            }

            if (currentToken.Type == JTokenType.Array)
            {
                MustacheParsingUtil.EvaluateJToken(writer, currentToken as JArray, context, this);
                return;
            }
            else if (currentToken.Type == JTokenType.Object)
            {
                // Set context to the object
                context = new MustacheEvaluationContext
                {
                    ReplacementObject = currentToken,
                    ParentContext = context,
                    PartialExpressions = context.PartialExpressions,
                    AdditionalEvaluationData = context.AdditionalEvaluationData,
                    Options = context.Options
                };
                MustacheEvaluationContext.CombinePartialsDictionaries(context, this);
            }

            base.Evaluate(context, writer);
        }

        private IList<Object> ParseArgumentExpressions()
        {
            List<Object> argumentExpressions = new List<Object>();

            if (!String.IsNullOrEmpty(Expression))
            {
                for (int i = 0; i < Expression.Length; i++)
                {
                    char c = Expression[i];

                    if (c == '\"' || c == '\'')
                    {
                        // String literal: begins with a quote character
                        StringBuilder stringLiteral = new StringBuilder();

                        i++;
                        // Iterate to find the end of the string literal (i.e. unescaped matching quote)
                        while (i < Expression.Length)
                        {
                            char c2 = Expression[i];

                            // Check for end of the string literal (matching start and end quote)
                            if (c2 == c)
                            {
                                break;
                            }
                            // Check for escaped quotes within the string
                            else if (c2 == '\\')
                            {
                                i++;
                                char c3 = MustacheParsingUtil.SafeCharAt(Expression, i);

                                if (c3 == '\"' || c3 == '\'' || c3 == '\\')
                                {

                                    stringLiteral.Append(c3);
                                }
                                else
                                {
                                    throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateInvalidEscapedStringLiteral(stringLiteral.ToString(), Expression));
                                }
                            }
                            else
                            {
                                stringLiteral.Append(c2);
                            }

                            i++;

                            if (i == Expression.Length)
                            {
                                throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateUnterminatedStringLiteral(c + stringLiteral.ToString(), Expression));
                            }
                        }

                        argumentExpressions.Add(stringLiteral.ToString());

                        continue;
                    }
                    else if (Char.IsDigit(c) || c == '-')
                    {
                        // Numerical value
                        StringBuilder numericLiteral = new StringBuilder().Append(c);

                        // Iterate to find the end of the number
                        i++;
                        while (i < Expression.Length)
                        {
                            char c2 = Expression[i];

                            if (Char.IsDigit(c2) || c2 == '.')
                            {
                                numericLiteral.Append(c2);
                            }
                            else if (c2 == ' ')
                            {
                                break;
                            }
                            else
                            {
                                // not really a numeric literal, unwind since this could be a valid non-literal
                                i = i - numericLiteral.Length;
                                numericLiteral = null;
                                break;
                            }

                            i++;
                        }

                        if (numericLiteral != null && !numericLiteral.ToString().Equals("-"))
                        {
                            String numericLiteralString = numericLiteral.ToString();

                            try
                            {
                                if (numericLiteralString.IndexOf('.') > -1)
                                {
                                    argumentExpressions.Add(Double.Parse(numericLiteralString));
                                }
                                else
                                {
                                    argumentExpressions.Add(Int32.Parse(numericLiteralString));
                                }
                            }
                            catch (Exception)
                            {
                                throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateInvalidNumericLiteral(numericLiteralString, Expression));
                            }

                            continue;
                        }
                    }
                    else if (MustacheParsingUtil.IsConstantAt(Expression, i, "null"))
                    {
                        // <null> literal
                        argumentExpressions.Add(null);

                        i += 4;
                        continue;
                    }
                    else if (MustacheParsingUtil.IsConstantAt(Expression, i, Boolean.TrueString))
                    {
                        // <true> liternal
                        argumentExpressions.Add(true);

                        i += Boolean.TrueString.Length;
                        continue;
                    }
                    else if (MustacheParsingUtil.IsConstantAt(Expression, i, Boolean.FalseString))
                    {
                        // <false> liternal
                        argumentExpressions.Add(false);

                        i += Boolean.FalseString.Length;
                        continue;
                    }
                    else if (c == ' ')
                    {
                        continue;
                    }

                    // selector argument
                    StringBuilder selector = new StringBuilder().Append(c);
                    i++;
                    while (i < Expression.Length)
                    {
                        char c2 = Expression[i];
                        if (c2 != ' ')
                        {
                            selector.Append(c2);
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Max depth does not need to be validated for the selector expression. Consumers always treat the
                    // argument expression as a selector only. Refer to consumers of the property HelperArguments.
                    MustacheTemplatedExpression selectorExpression = new MustacheTemplatedExpression
                    (
                        selector.ToString(), // expression
                        ParentExpression,
                        RootExpression
                    );

                    argumentExpressions.Add(selectorExpression);
                }
            }

            return argumentExpressions;
        }

        /// <summary>
        /// For a templated expression using a Helper, this gets the raw string argument at the nth position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public String GetRawHelperArgument(int index)
        {
            IList<Object> argumentExpressions = HelperArguments;
            if (argumentExpressions.Count > index)
            {
                Object value = argumentExpressions[index];
                if (value != null)
                {
                    MustacheTemplatedExpression templatedExpression = value as MustacheTemplatedExpression;
                    if (templatedExpression != null)
                    {
                        return templatedExpression.Expression;
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// For a templated expression using a Helper, this gives the appropriate value for the nth argument to the helper.
        /// Non-quoted strings (which aren't valid literal values) are resolved as context selectors.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetHelperArgument<T>(MustacheEvaluationContext context, int index, T defaultValue = default(T))
        {
            T argumentValue = defaultValue;

            IList<Object> argumentExpressions = HelperArguments;
            if (argumentExpressions != null)
            {
                if (index >= 0 && index < argumentExpressions.Count)
                {
                    Object rawValue = argumentExpressions[index];
                    if (rawValue != null)
                    {
                        MustacheTemplatedExpression templatedExpression = rawValue as MustacheTemplatedExpression;
                        if (templatedExpression != null)
                        {
                            try
                            {
                                JToken token = GetCurrentJToken(templatedExpression.Expression, context);
                                if (token != null)
                                {
                                    argumentValue = token.Value<T>();
                                }
                            }
                            catch (Exception)
                            {
                                // best effort conversion - return default value
                            }
                        }
                        else if (rawValue is T)
                        {
                            argumentValue = (T)rawValue;
                        }
                        else
                        {
                            try
                            {
                                TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                                if (converter.CanConvertFrom(rawValue.GetType()))
                                {
                                    argumentValue = (T)converter.ConvertFrom(rawValue);
                                }
                                else
                                {
                                    converter = TypeDescriptor.GetConverter(rawValue.GetType());
                                    if (converter.CanConvertTo(typeof(T)))
                                    {
                                        argumentValue = (T)converter.ConvertTo(rawValue, typeof(T));
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                // best effort attempt to convert the argument value
                            }
                        }
                    }
                }
            }

            return argumentValue;
        }

        internal static Boolean IsHelperArgumentTruthy(MustacheEvaluationContext context, Object argValue)
        {
            MustacheTemplatedExpression templatedExpression = argValue as MustacheTemplatedExpression;
            if (templatedExpression != null)
            {
                JToken token = templatedExpression.GetCurrentJToken(templatedExpression.Expression, context);
                return templatedExpression.IsTokenTruthy(token);
            }
            else
            {
                if (argValue is Double d)
                {
                    return (d != 0);
                }
                else if (argValue is Int32 i)
                {
                    return (i != 0);
                }
                else if (argValue is Boolean b)
                {
                    return b;
                }
                else if (argValue is String s)
                {
                    return !String.IsNullOrEmpty(s);
                }
                else
                {
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Common implementations to use for the delegate <see cref="MustacheEncodeMethod"/>.
    /// </summary>
    public static class MustacheEncodeMethods
    {
        /// <summary>
        /// Applies HTML encoding rules to the value.
        /// </summary>
        /// <param name="value">value to be encoded</param>
        /// <returns>the HTML-encoded value</returns>
        public static String HtmlEncode(String value)
        {
            return UriUtility.HtmlEncode(value);
        }

        /// <summary>
        /// Applies JSON-string encoding rules to the value. Note, the value is simply escaped and additional surrounding quotes are not added.
        /// </summary>
        /// <param name="value">value to be encoded</param>
        /// <returns>the JSON-string-encoded value</returns>
        public static String JsonEncode(String value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }
            else
            {
                String jsonString = JsonConvert.ToString(value);

                // Remove leading/trailing double-quote
                return jsonString.Substring(startIndex: 1, length: jsonString.Length - 2);
            }
        }

        /// <summary>
        /// Simply returns the string without applying any encoding rules.
        /// </summary>
        /// <param name="value">value to be encoded</param>
        /// <returns>the original value</returns>
        public static String NoEncode(String value)
        {
            return value;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class MustacheParsingUtil
    {
        internal static char SafeCharAt(String value, int index)
        {
            if (index >= 0 && index < value.Length)
            {
                return value[index];
            }
            else
            {
                return '\0';
            }
        }

        internal static String SafeSubstring(String value, int index, int length)
        {
            if (index >= 0 && length > 0 && index + length <= value.Length)
            {
                return value.Substring(index, length);
            }
            else
            {
                return "";
            }
        }

        internal static bool IsConstantAt(String value, int index, String constantValue)
        {
            if (String.Equals(SafeSubstring(value, index, constantValue.Length), constantValue, StringComparison.OrdinalIgnoreCase))
            {
                char endChar = MustacheParsingUtil.SafeCharAt(value, index + constantValue.Length);
                if (endChar == ' ' || endChar == '\0')
                {
                    return true;
                }
            }

            return false;
        }
        
        internal static void EvaluateJToken(MustacheTextWriter writer, JArray token, MustacheEvaluationContext context, MustacheTemplatedExpression expression)
        {
            // Handle array values - Enumerate through array (each)
            for (int index = 0; index < token.Count; index++)
            {
                MustacheEvaluationContext childContext = new MustacheEvaluationContext
                {
                    ReplacementObject = token[index],
                    ParentContext = context,
                    CurrentIndex = index,
                    ParentItemsCount = token.Count,
                    PartialExpressions = context.PartialExpressions,
                    AdditionalEvaluationData = context.AdditionalEvaluationData,
                    Options = context.Options
                };
                MustacheEvaluationContext.CombinePartialsDictionaries(childContext, expression);
                expression.EvaluateChildExpressions(childContext, writer);
            }
        }

        internal static void AssertDepth(MustacheOptions options, Int32 depth)
        {
            if (options.MaxDepth > 0 && depth > options.MaxDepth)
            {
                throw new MustacheExpressionInvalidException(WebApiResources.MustacheTemplateMaxDepthExceeded(options.MaxDepth));
            }
        }
    }
}
