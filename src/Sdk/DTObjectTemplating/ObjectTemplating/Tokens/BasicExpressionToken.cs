using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Functions;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.Services.WebApi.Internal;
using Container = GitHub.DistributedTask.Expressions2.Sdk.Container;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class BasicExpressionToken : ExpressionToken
    {
        internal BasicExpressionToken(
            Int32? fileId,
            Int32? line,
            Int32? column,
            String expression)
            : base(TokenType.BasicExpression, fileId, line, column, null)
        {
            m_expression = expression;
        }

        internal String Expression
        {
            get
            {
                if (m_expression == null)
                {
                    m_expression = String.Empty;
                }

                return m_expression;
            }
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new BasicExpressionToken(null, null, null, m_expression) : new BasicExpressionToken(FileId, Line, Column, m_expression);
        }

        public override String ToString()
        {
            return $"{TemplateConstants.OpenExpression} {m_expression} {TemplateConstants.CloseExpression}";
        }

        public override String ToDisplayString()
        {
            var expressionParser = new ExpressionParser();
            var expressionNode = expressionParser.ValidateSyntax(Expression, null);
            if (expressionNode is Format formatNode)
            {
                // Make sure our first item is indeed a literal string so we can format it.
                if (formatNode.Parameters.Count > 1 &&
                    formatNode.Parameters.First() is Literal literalValueNode &&
                    literalValueNode.Kind == ValueKind.String)
                {
                    // Get all other Parameters san the formatted string to pass into the formatter
                    var formatParameters = formatNode.Parameters.Skip(1).Select(x => this.ConvertFormatParameterToExpression(x)).ToArray();
                    if (formatParameters.Length > 0)
                    {
                        String formattedString = String.Empty;
                        try
                        {
                            formattedString = String.Format(CultureInfo.InvariantCulture, (formatNode.Parameters[0] as Literal).Value as String, formatParameters);
                        }
                        catch (FormatException) { }
                        catch (ArgumentNullException) { } // If this operation fails, revert to default display name
                        if (!String.IsNullOrEmpty(formattedString))
                        {
                            return TrimDisplayString(formattedString);
                        }
                    }
                }
            }
            return base.ToDisplayString();
        }

        internal StringToken EvaluateStringToken(
            TemplateContext context,
            out Int32 bytes)
        {
            return EvaluateStringToken(context, Expression, out bytes);
        }

        internal MappingToken EvaluateMappingToken(
            TemplateContext context,
            out Int32 bytes)
        {
            return EvaluateMappingToken(context, Expression, out bytes);
        }

        internal SequenceToken EvaluateSequenceToken(
            TemplateContext context,
            out Int32 bytes)
        {
            return EvaluateSequenceToken(context, Expression, out bytes);
        }

        internal TemplateToken EvaluateTemplateToken(
            TemplateContext context,
            out Int32 bytes)
        {
            return EvaluateTemplateToken(context, Expression, out bytes);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_expression?.Length == 0)
            {
                m_expression = null;
            }
        }

        private String ConvertFormatParameterToExpression(ExpressionNode node)
        {
            var nodeString = node.ConvertToExpression();

            // If the node is a container, see if it starts with '(' and ends with ')' so we can simplify the string
            // Should only simplify if only one '(' or ')' exists in the string
            // We are trying to simplify the case (a || b) to a || b
            // But we should avoid simplifying ( a && b
            if (node is Container &&
                nodeString.Length > 2 &&
                nodeString[0] == ExpressionConstants.StartParameter &&
                nodeString[nodeString.Length - 1] == ExpressionConstants.EndParameter &&
                nodeString.Count(character => character == ExpressionConstants.StartParameter) == 1 &&
                nodeString.Count(character => character == ExpressionConstants.EndParameter) == 1)
            {
                nodeString = nodeString = nodeString.Substring(1, nodeString.Length - 2);
            }
            return String.Concat(TemplateConstants.OpenExpression, " ", nodeString, " ", TemplateConstants.CloseExpression);
        }

        [DataMember(Name = "expr", EmitDefaultValue = false)]
        private String m_expression;
    }
}
