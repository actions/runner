using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2.Tokens;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
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
                    description = $"Exceeded max expression depth {ExpressionConstants.MaxDepth}";
                    break;
                case ParseExceptionKind.ExceededMaxLength:
                    description = $"Exceeded max expression length {ExpressionConstants.MaxLength}";
                    break;
                case ParseExceptionKind.TooFewParameters:
                    description = "Too few parameters supplied";
                    break;
                case ParseExceptionKind.TooManyParameters:
                    description = "Too many parameters supplied";
                    break;
                case ParseExceptionKind.UnexpectedEndOfExpression:
                    description = "Unexpected end of expression";
                    break;
                case ParseExceptionKind.UnexpectedSymbol:
                    description = "Unexpected symbol";
                    break;
                case ParseExceptionKind.UnrecognizedFunction:
                    description = "Unrecognized function";
                    break;
                case ParseExceptionKind.UnrecognizedNamedValue:
                    description = "Unrecognized named-value";
                    break;
                default: // Should never reach here.
                    throw new Exception($"Unexpected parse exception kind '{kind}'.");
            }

            if (token == null)
            {
                Message = description;
            }
            else
            {
                Message = $"{description}: '{RawToken}'. Located at position {TokenIndex + 1} within expression: {Expression}";
            }
        }

        internal String Expression { get; }

        internal ParseExceptionKind Kind { get; }

        internal String RawToken { get; }

        internal Int32 TokenIndex { get; }

        public sealed override String Message { get; }
    }
}
