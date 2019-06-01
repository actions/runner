using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens
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

        public override TemplateToken Clone()
        {
            return Clone(false);
        }

        public override TemplateToken Clone(Boolean omitSource)
        {
            return omitSource ? new BasicExpressionToken(null, null, null, m_expression) : new BasicExpressionToken(FileId, Line, Column, m_expression);
        }

        public override String ToString()
        {
            return $"${{{{ {m_expression} }}}}";
        }

        internal LiteralToken EvaluateLiteralToken(
            TemplateContext context,
            out Int32 bytes)
        {
            return EvaluateLiteralToken(context, Expression, out bytes);
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
            return EvaluateTemplateToken(context, Expression, true, out bytes);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_expression?.Length == 0)
            {
                m_expression = null;
            }
        }

        [DataMember(Name = "expr", EmitDefaultValue = false)]
        private String m_expression;
    }
}