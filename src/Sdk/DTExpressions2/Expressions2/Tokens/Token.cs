using System;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Operators;

namespace GitHub.DistributedTask.Expressions2.Tokens
{
    internal sealed class Token
    {
        public Token(
            TokenKind kind,
            String rawValue,
            Int32 index,
            Object parsedValue = null)
        {
            Kind = kind;
            RawValue = rawValue;
            Index = index;
            ParsedValue = parsedValue;
        }

        public TokenKind Kind { get; }

        public String RawValue { get; }

        public Int32 Index { get; }

        public Object ParsedValue { get; }

        public Associativity Associativity
        {
            get
            {
                switch (Kind)
                {
                    case TokenKind.StartGroup:
                        return Associativity.None;
                    case TokenKind.LogicalOperator:
                        switch (RawValue)
                        {
                            case ExpressionConstants.Not:   // "!"
                                return Associativity.RightToLeft;
                        }
                        break;
                }

                return IsOperator ? Associativity.LeftToRight : Associativity.None;
            }
        }

        public Boolean IsOperator
        {
            get
            {
                switch (Kind)
                {
                    case TokenKind.StartGroup:      // "(" logical grouping
                    case TokenKind.StartIndex:      // "["
                    case TokenKind.StartParameters: // "(" function call
                    case TokenKind.EndGroup:        // ")" logical grouping
                    case TokenKind.EndIndex:        // "]"
                    case TokenKind.EndParameters:   // ")" function call
                    case TokenKind.Separator:       // ","
                    case TokenKind.Dereference:     // "."
                    case TokenKind.LogicalOperator: // "!", "==", etc
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Operator precedence. The value is only meaningful for operator tokens.
        /// </summary>
        public Int32 Precedence
        {
            get
            {
                switch (Kind)
                {
                    case TokenKind.StartGroup:      // "(" logical grouping
                        return 20;
                    case TokenKind.StartIndex:      // "["
                    case TokenKind.StartParameters: // "(" function call
                    case TokenKind.Dereference:     // "."
                        return 19;
                    case TokenKind.LogicalOperator:
                        switch (RawValue)
                        {
                            case ExpressionConstants.Not:               // "!"
                                return 16;
                            case ExpressionConstants.GreaterThan:       // ">"
                            case ExpressionConstants.GreaterThanOrEqual:// ">="
                            case ExpressionConstants.LessThan:          // "<"
                            case ExpressionConstants.LessThanOrEqual:   // "<="
                                return 11;
                            case ExpressionConstants.Equal:             // "=="
                            case ExpressionConstants.NotEqual:          // "!="
                                return 10;
                            case ExpressionConstants.And:               // "&&"
                                return 6;
                            case ExpressionConstants.Or:                // "||"
                                return 5;
                        }
                        break;
                    case TokenKind.EndGroup:        // ")" logical grouping
                    case TokenKind.EndIndex:        // "]"
                    case TokenKind.EndParameters:   // ")" function call
                    case TokenKind.Separator:       // ","
                        return 1;
                }

                return 0;
            }
        }

        /// <summary>
        /// Expected number of operands. The value is only meaningful for standalone unary operators and binary operators.
        /// </summary>
        public Int32 OperandCount
        {
            get
            {
                switch (Kind)
                {
                    case TokenKind.StartIndex:      // "["
                    case TokenKind.Dereference:     // "."
                        return 2;
                    case TokenKind.LogicalOperator:
                        switch (RawValue)
                        {
                            case ExpressionConstants.Not:               // "!"
                                return 1;
                            case ExpressionConstants.GreaterThan:       // ">"
                            case ExpressionConstants.GreaterThanOrEqual:// ">="
                            case ExpressionConstants.LessThan:          // "<"
                            case ExpressionConstants.LessThanOrEqual:   // "<="
                            case ExpressionConstants.Equal:             // "=="
                            case ExpressionConstants.NotEqual:          // "!="
                            case ExpressionConstants.And:               // "&&"
                            case ExpressionConstants.Or:                // "|"
                                return 2;
                        }
                        break;
                }

                return 0;
            }
        }

        public ExpressionNode ToNode()
        {
            switch (Kind)
            {
                case TokenKind.StartIndex:          // "["
                case TokenKind.Dereference:         // "."
                    return new Sdk.Operators.Index();

                case TokenKind.LogicalOperator:
                    switch (RawValue)
                    {
                        case ExpressionConstants.Not:               // "!"
                            return new Not();

                        case ExpressionConstants.NotEqual:          // "!="
                            return new NotEqual();

                        case ExpressionConstants.GreaterThan:       // ">"
                            return new GreaterThan();

                        case ExpressionConstants.GreaterThanOrEqual:// ">="
                            return new GreaterThanOrEqual();

                        case ExpressionConstants.LessThan:          // "<"
                            return new LessThan();

                        case ExpressionConstants.LessThanOrEqual:   // "<="
                            return new LessThanOrEqual();

                        case ExpressionConstants.Equal:             // "=="
                            return new Equal();

                        case ExpressionConstants.And:               // "&&"
                            return new And();

                        case ExpressionConstants.Or:                // "||"
                            return new Or();

                        default:
                            throw new NotSupportedException($"Unexpected logical operator '{RawValue}' when creating node");
                    }

                case TokenKind.Null:
                case TokenKind.Boolean:
                case TokenKind.Number:
                case TokenKind.String:
                    return new Literal(ParsedValue);

                case TokenKind.PropertyName:
                    return new Literal(RawValue);

                case TokenKind.Wildcard:            // "*"
                    return new Wildcard();
            }

            throw new NotSupportedException($"Unexpected kind '{Kind}' when creating node");
        }
    }
}
