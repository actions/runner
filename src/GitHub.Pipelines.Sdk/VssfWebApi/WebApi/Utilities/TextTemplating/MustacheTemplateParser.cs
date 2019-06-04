using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Utility class for working with mustache-style templates
    /// </summary>
    public class MustacheTemplateParser
    {
        private static Dictionary<String, MustacheTemplateHelperWriter> s_defaultHandlebarHelpers = HandleBarBuiltinHelpers.GetHelpers();
        private static Dictionary<String, MustacheTemplateHelperWriter> s_commonHelpers = CommonMustacheHelpers.GetHelpers();

        private Dictionary<String, MustacheTemplateHelperWriter> m_helpers;
        private Dictionary<String, MustacheRootExpression> m_partials;
        private MustacheOptions m_options;

        /// <summary>
        /// Template helpers to use when evaluating expressions
        /// </summary>
        [Obsolete("Use the RegisterHelper method")]
        public Dictionary<String, MustacheTemplateHelper> Helpers { get; private set; }

        /// <summary>
        /// Template block helpers to use when evaluating expressions
        /// </summary>
        [Obsolete("Use the RegisterHelper method")]
        public Dictionary<String, MustacheTemplateHelper> BlockHelpers { get; private set; }

        /// <summary
        /// Externally defined partial templates
        /// </summary>
        [Obsolete("Use the RegisterPartial method")]
        public Dictionary<String, MustacheRootExpression> Partials { get; private set; }

        /// <summary>
        /// Create a helper for parsing mustache templates
        /// </summary>
        /// <param name="useDefaultHandlebarHelpers">Register handlebar helpers (with, if, else, etc.)</param>
        /// <param name="useCommonHandlebarHelpers">Register common template helpers (equals, notequals, etc.)</param>
        /// <param name="partials">Register partial expressions</param>
        public MustacheTemplateParser(
            bool useDefaultHandlebarHelpers = true,
            Dictionary<String, String> partials = null)
            : this(useDefaultHandlebarHelpers, true, partials, null)
        {
        }

        /// <summary>
        /// Create a helper for parsing mustache templates
        /// </summary>
        /// <param name="useDefaultHandlebarHelpers">Register handlebar helpers (with, if, else, etc.)</param>
        /// <param name="useCommonHandlebarHelpers">Register common template helpers (equals, notequals, etc.)</param>
        public MustacheTemplateParser(
            bool useDefaultHandlebarHelpers,
            bool useCommonTemplateHelpers)
            : this(useDefaultHandlebarHelpers, useCommonTemplateHelpers, null, null)
        {
        }

        /// <summary>
        /// Create a helper for parsing mustache templates
        /// </summary>
        /// <param name="useDefaultHandlebarHelpers">Register handlebar helpers (with, if, else, etc.)</param>
        /// <param name="useCommonHandlebarHelpers">Register common template helpers (equals, notequals, etc.)</param>
        /// <param name="partials">Register partial expressions</param>
        /// <param name="options">Options to use for parsing and evaluation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public MustacheTemplateParser(
            bool useDefaultHandlebarHelpers,
            bool useCommonTemplateHelpers,
            Dictionary<String, String> partials,
            MustacheOptions options)
        {
            // Store the options before parsing partials.
            m_options = options;

            if (useDefaultHandlebarHelpers)
            {
                m_helpers = new Dictionary<String, MustacheTemplateHelperWriter>(s_defaultHandlebarHelpers, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                m_helpers = new Dictionary<String, MustacheTemplateHelperWriter>(StringComparer.OrdinalIgnoreCase);
            }

            if (useCommonTemplateHelpers)
            {
                foreach (KeyValuePair<String, MustacheTemplateHelperWriter> helper in s_commonHelpers)
                {
                    m_helpers[helper.Key] = helper.Value;
                }
            }

            m_partials = new Dictionary<String, MustacheRootExpression>(StringComparer.OrdinalIgnoreCase);
            if (partials != null)
            {
                foreach (KeyValuePair<String, String> partial in partials)
                {
                    m_partials[partial.Key] = (MustacheRootExpression)Parse(partial.Value);
                }
            }
        }

        /// <summary>
        /// Register the helper with the specified name
        /// </summary>
        /// <param name="helperName"></param>
        /// <param name="helper"></param>
        public void RegisterHelper(String helperName, MustacheTemplateHelperMethod helper)
        {
            m_helpers[helperName] = (MustacheTemplatedExpression expression, MustacheTextWriter writer, MustacheEvaluationContext context) =>
            {
                Object result = helper(expression, context);
                if (result != null)
                {
                    writer.Write(result.ToString());
                }
            };
        }

        /// <summary>
        /// Register the helper with the specified name
        /// </summary>
        /// <param name="helperName"></param>
        /// <param name="helper"></param>
        public void RegisterHelper(String helperName, MustacheTemplateHelperWriter helper)
        {
            m_helpers[helperName] = helper;
        }

        /// <summary>
        /// Register a new partial template in string form with the template parser
        /// Overwrites an existing partial with the same name
        /// </summary>
        /// <param name="partialName"></param>
        /// <param name="partialExpression"></param>
        public void ParseAndRegisterPartial(String partialName, String partialExpression)
        {
            m_partials[partialName] = (MustacheRootExpression)Parse(partialExpression);
        }

        /// <summary>
        /// Register a new partial template in mustache-expression-tree form with the template parser
        /// Overwrites an existing partial with the same name
        /// </summary>
        /// <param name="partialName"></param>
        /// <param name="partialExpression"></param>
        public void RegisterPartial(String partialName, MustacheRootExpression partialExpression)
        {
            m_partials[partialName] = partialExpression;
        }

        /// <summary>
        /// Repace values in a mustache-style template with values from the given property bag.
        /// </summary>
        /// <param name="template">mustache-style template</param>
        /// <param name="replacementContext">properties to use as replacements</param>
        /// <returns></returns>
        public String ReplaceValues(String template, Object replacementContext)
        {
            MustacheRootExpression expression = MustacheExpression.Parse(template, m_helpers, m_partials, m_options);
            return expression.Evaluate(
                replacementObject: replacementContext,
                additionalEvaluationData: null,
                parentContext: null,
                partialExpressions: null,
                options: m_options);
        }

        /// <summary>
        /// Parse the given mustache template, resulting in a "compiled" expression that can
        /// be evaluated with a replacement context
        /// </summary>
        /// <param name="template">mustache-style template</param>
        /// <returns></returns>
        public MustacheExpression Parse(String template)
        {
            return MustacheExpression.Parse(template, m_helpers, m_partials, m_options);
        }
    }
}
