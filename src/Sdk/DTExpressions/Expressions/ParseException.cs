using System;

namespace GitHub.DistributedTask.Expressions
{
    public sealed class ParseException : ExpressionException
    {
        internal ParseException(ParseExceptionKind kind, Token token, String expression)
            : base(secretMasker: null, message: String.Empty)
        {
            Expression = expression;
            Kind = kind;
            RawToken = token?.RawValue;
            TokenIndex = token?.Index ?? 0;
            String description;
            switch (kind)
            {
                case ParseExceptionKind.ExceededMaxDepth:
                    description = ExpressionResources.ExceededMaxExpressionDepth(ExpressionConstants.MaxDepth);
                    break;
                case ParseExceptionKind.ExceededMaxLength:
                    description = ExpressionResources.ExceededMaxExpressionLength(ExpressionConstants.MaxLength);
                    break;
                case ParseExceptionKind.ExpectedPropertyName:
                    description = ExpressionResources.ExpectedPropertyName();
                    break;
                case ParseExceptionKind.ExpectedStartParameter:
                    description = ExpressionResources.ExpectedStartParameter();
                    break;
                case ParseExceptionKind.UnclosedFunction:
                    description = ExpressionResources.UnclosedFunction();
                    break;
                case ParseExceptionKind.UnclosedIndexer:
                    description = ExpressionResources.UnclosedIndexer();
                    break;
                case ParseExceptionKind.UnexpectedSymbol:
                    description = ExpressionResources.UnexpectedSymbol();
                    break;
                case ParseExceptionKind.UnrecognizedValue:
                    description = ExpressionResources.UnrecognizedValue();
                    break;
                default: // Should never reach here.
                    throw new Exception($"Unexpected parse exception kind '{kind}'.");
            }

            if (token == null)
            {
                Message = ExpressionResources.ParseErrorWithFwlink(description);
            }
            else
            {
                Message = ExpressionResources.ParseErrorWithTokenInfo(description, RawToken, TokenIndex + 1, Expression);
            }
        }

        internal String Expression { get; }

        internal ParseExceptionKind Kind { get; }

        internal String RawToken { get; }

        internal Int32 TokenIndex { get; }

        public sealed override String Message { get; }
    }
}
