using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.Expressions2.Sdk.Operators;
using GitHub.DistributedTask.Expressions2.Tokens;

namespace GitHub.DistributedTask.Expressions2
{
    using GitHub.DistributedTask.Expressions2.Sdk;
    using GitHub.DistributedTask.Expressions2.Sdk.Functions;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ExpressionParser
    {
        public IExpressionNode CreateTree(
            String expression,
            ITraceWriter trace,
            IEnumerable<INamedValueInfo> namedValues,
            IEnumerable<IFunctionInfo> functions)
        {
            var context = new ParseContext(expression, trace, namedValues, functions);
            context.Trace.Info($"Parsing expression: <{expression}>");
            return CreateTree(context);
        }

        public IExpressionNode ValidateSyntax(
            String expression,
            ITraceWriter trace)
        {
            var context = new ParseContext(expression, trace, namedValues: null, functions: null, allowUnknownKeywords: true);
            context.Trace.Info($"Validating expression syntax: <{expression}>");
            return CreateTree(context);
        }

        private static IExpressionNode CreateTree(ParseContext context)
        {
            // Push the tokens
            while (context.LexicalAnalyzer.TryGetNextToken(ref context.Token))
            {
                // Unexpected
                if (context.Token.Kind == TokenKind.Unexpected)
                {
                    throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
                }
                // Operator
                else if (context.Token.IsOperator)
                {
                    PushOperator(context);
                }
                // Operand
                else
                {
                    PushOperand(context);
                }

                context.LastToken = context.Token;
            }

            // No tokens
            if (context.LastToken == null)
            {
                return null;
            }

            // Check unexpected end of expression
            if (context.Operators.Count > 0)
            {
                var unexpectedLastToken = false;
                switch (context.LastToken.Kind)
                {
                    case TokenKind.EndGroup:        // ")" logical grouping
                    case TokenKind.EndIndex:        // "]"
                    case TokenKind.EndParameters:   // ")" function call
                        // Legal
                        break;
                    case TokenKind.Function:
                        // Illegal
                        unexpectedLastToken = true;
                        break;
                    default:
                        unexpectedLastToken = context.LastToken.IsOperator;
                        break;
                }

                if (unexpectedLastToken || context.LexicalAnalyzer.UnclosedTokens.Any())
                {
                    throw new ParseException(ParseExceptionKind.UnexpectedEndOfExpression, context.LastToken, context.Expression);
                }
            }

            // Flush operators
            while (context.Operators.Count > 0)
            {
                FlushTopOperator(context);
            }

            // Check max depth
            var result = context.Operands.Single();
            CheckMaxDepth(context, result);

            return result;
        }

        private static void PushOperand(ParseContext context)
        {
            // Create the node
            var node = default(ExpressionNode);
            switch (context.Token.Kind)
            {
                // Function
                case TokenKind.Function:
                    var function = context.Token.RawValue;
                    if (TryGetFunctionInfo(context, function, out var functionInfo))
                    {
                        node = functionInfo.CreateNode();
                        node.Name = function;
                    }
                    else if (context.AllowUnknownKeywords)
                    {
                        node = new NoOperation();
                        node.Name = function;
                    }
                    else
                    {
                        throw new ParseException(ParseExceptionKind.UnrecognizedFunction, context.Token, context.Expression);
                    }
                    break;

                // Named-value
                case TokenKind.NamedValue:
                    var name = context.Token.RawValue;
                    if (context.ExtensionNamedValues.TryGetValue(name, out var namedValueInfo))
                    {
                        node = namedValueInfo.CreateNode();
                        node.Name = name;

                    }
                    else if (context.AllowUnknownKeywords)
                    {
                        node = new NoOperationNamedValue();
                        node.Name = name;
                    }
                    else
                    {
                        throw new ParseException(ParseExceptionKind.UnrecognizedNamedValue, context.Token, context.Expression);
                    }
                    break;

                // Otherwise simple
                default:
                    node = context.Token.ToNode();
                    break;
            }

            // Push the operand
            context.Operands.Push(node);
        }

        private static void PushOperator(ParseContext context)
        {
            // Flush higher or equal precedence
            if (context.Token.Associativity == Associativity.LeftToRight)
            {
                var precedence = context.Token.Precedence;
                while (context.Operators.Count > 0)
                {
                    var topOperator = context.Operators.Peek();
                    if (precedence <= topOperator.Precedence &&
                        topOperator.Kind != TokenKind.StartGroup &&     // Unless top is "(" logical grouping
                        topOperator.Kind != TokenKind.StartIndex &&     // or unless top is "["
                        topOperator.Kind != TokenKind.StartParameters &&// or unless top is "(" function call
                        topOperator.Kind != TokenKind.Separator)        // or unless top is ","
                    {
                        FlushTopOperator(context);
                        continue;
                    }

                    break;
                }
            }

            // Push the operator
            context.Operators.Push(context.Token);

            // Process closing operators now, since context.LastToken is required
            // to accurately process TokenKind.EndParameters
            switch (context.Token.Kind)
            {
                case TokenKind.EndGroup:        // ")" logical grouping
                case TokenKind.EndIndex:        // "]"
                case TokenKind.EndParameters:   // ")" function call
                    FlushTopOperator(context);
                    break;
            }
        }

        private static void FlushTopOperator(ParseContext context)
        {
            // Special handling for closing operators
            switch (context.Operators.Peek().Kind)
            {
                case TokenKind.EndIndex:        // "]"
                    FlushTopEndIndex(context);
                    return;

                case TokenKind.EndGroup:        // ")" logical grouping
                    FlushTopEndGroup(context);
                    return;

                case TokenKind.EndParameters:   // ")" function call
                    FlushTopEndParameters(context);
                    return;
            }

            // Pop the operator
            var @operator = context.Operators.Pop();

            // Create the node
            var node = (Container)@operator.ToNode();

            // Pop the operands, add to the node
            var operands = PopOperands(context, @operator.OperandCount);
            foreach (var operand in operands)
            {
                // Flatten nested And
                if (node is And)
                {
                    if (operand is And nestedAnd)
                    {
                        foreach (var nestedParameter in nestedAnd.Parameters)
                        {
                            node.AddParameter(nestedParameter);
                        }

                        continue;
                    }
                }
                // Flatten nested Or
                else if (node is Or)
                {
                    if (operand is Or nestedOr)
                    {
                        foreach (var nestedParameter in nestedOr.Parameters)
                        {
                            node.AddParameter(nestedParameter);
                        }

                        continue;
                    }
                }

                node.AddParameter(operand);
            }

            // Push the node to the operand stack
            context.Operands.Push(node);
        }

        /// <summary>
        /// Flushes the ")" logical grouping operator
        /// </summary>
        private static void FlushTopEndGroup(ParseContext context)
        {
            // Pop the operators
            PopOperator(context, TokenKind.EndGroup);   // ")" logical grouping
            PopOperator(context, TokenKind.StartGroup); // "(" logical grouping
        }

        /// <summary>
        /// Flushes the "]" operator
        /// </summary>
        private static void FlushTopEndIndex(ParseContext context)
        {
            // Pop the operators
            PopOperator(context, TokenKind.EndIndex);                   // "]"
            var @operator = PopOperator(context, TokenKind.StartIndex);  // "["

            // Create the node
            var node = (Container)@operator.ToNode();

            // Pop the operands, add to the node
            var operands = PopOperands(context, @operator.OperandCount);
            foreach (var operand in operands)
            {
                node.AddParameter(operand);
            }

            // Push the node to the operand stack
            context.Operands.Push(node);
        }

        // ")" function call
        private static void FlushTopEndParameters(ParseContext context)
        {
            // Pop the operator
            var @operator = PopOperator(context, TokenKind.EndParameters);   // ")" function call

            // Sanity check top operator is the current token
            if (!Object.ReferenceEquals(@operator, context.Token))
            {
                throw new InvalidOperationException("Expected the operator to be the current token");
            }

            var function = default(Function);

            // No parameters
            if (context.LastToken.Kind == TokenKind.StartParameters)
            {
                // Node already exists on the operand stack
                function = (Function)context.Operands.Peek();
            }
            // Has parameters
            else
            {
                // Pop the operands
                var parameterCount = 1;
                while (context.Operators.Peek().Kind == TokenKind.Separator)
                {
                    parameterCount++;
                    context.Operators.Pop();
                }
                var functionOperands = PopOperands(context, parameterCount);
                
                // Node already exists on the operand stack
                function = (Function)context.Operands.Peek();

                // Add the operands to the node
                foreach (var operand in functionOperands)
                {
                    function.AddParameter(operand);
                }
            }

            // Pop the "(" operator too
            @operator = PopOperator(context, TokenKind.StartParameters);

            // Check min/max parameter count
            TryGetFunctionInfo(context, function.Name, out var functionInfo);
            if (functionInfo == null && context.AllowUnknownKeywords)
            {
                // Don't check min/max
            }
            else if (function.Parameters.Count < functionInfo.MinParameters)
            {
                throw new ParseException(ParseExceptionKind.TooFewParameters, token: @operator, expression: context.Expression);
            }
            else if (function.Parameters.Count > functionInfo.MaxParameters)
            {
                throw new ParseException(ParseExceptionKind.TooManyParameters, token: @operator, expression: context.Expression);
            }
        }

        /// <summary>
        /// Pops N operands from the operand stack. The operands are returned
        /// in their natural listed order, i.e. not last-in-first-out.
        /// </summary>
        private static List<ExpressionNode> PopOperands(
            ParseContext context,
            Int32 count)
        {
            var result = new List<ExpressionNode>();
            while (count-- > 0)
            {
                result.Add(context.Operands.Pop());
            }

            result.Reverse();
            return result;
        }

        /// <summary>
        /// Pops an operator and asserts it is the expected kind.
        /// </summary>
        private static Token PopOperator(
            ParseContext context,
            TokenKind expected)
        {
            var token = context.Operators.Pop();
            if (token.Kind != expected)
            {
                throw new NotSupportedException($"Expected operator '{expected}' to be popped. Actual '{token.Kind}'.");
            }
            return token;
        }

        /// <summary>
        /// Checks the max depth of the expression tree
        /// </summary>
        private static void CheckMaxDepth(
            ParseContext context,
            ExpressionNode node,
            Int32 depth = 1)
        {
            if (depth > ExpressionConstants.MaxDepth)
            {
                throw new ParseException(ParseExceptionKind.ExceededMaxDepth, token: null, expression: context.Expression);
            }

            if (node is Container container)
            {
                foreach (var parameter in container.Parameters)
                {
                    CheckMaxDepth(context, parameter, depth + 1);
                }
            }
        }

        private static Boolean TryGetFunctionInfo(
            ParseContext context,
            String name,
            out IFunctionInfo functionInfo)
        {
            return ExpressionConstants.WellKnownFunctions.TryGetValue(name, out functionInfo) ||
                context.ExtensionFunctions.TryGetValue(name, out functionInfo);
        }

        private sealed class ParseContext
        {
            public Boolean AllowUnknownKeywords;
            public readonly String Expression;
            public readonly Dictionary<String, IFunctionInfo> ExtensionFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<String, INamedValueInfo> ExtensionNamedValues = new Dictionary<String, INamedValueInfo>(StringComparer.OrdinalIgnoreCase);
            public readonly LexicalAnalyzer LexicalAnalyzer;
            public readonly Stack<ExpressionNode> Operands = new Stack<ExpressionNode>();
            public readonly Stack<Token> Operators = new Stack<Token>();
            public readonly ITraceWriter Trace;
            public Token Token;
            public Token LastToken;

            public ParseContext(
                String expression,
                ITraceWriter trace,
                IEnumerable<INamedValueInfo> namedValues,
                IEnumerable<IFunctionInfo> functions,
                Boolean allowUnknownKeywords = false)
            {
                Expression = expression ?? String.Empty;
                if (Expression.Length > ExpressionConstants.MaxLength)
                {
                    throw new ParseException(ParseExceptionKind.ExceededMaxLength, token: null, expression: Expression);
                }

                Trace = trace ?? new NoOperationTraceWriter();
                foreach (var namedValueInfo in (namedValues ?? new INamedValueInfo[0]))
                {
                    ExtensionNamedValues.Add(namedValueInfo.Name, namedValueInfo);
                }

                foreach (var functionInfo in (functions ?? new IFunctionInfo[0]))
                {
                    ExtensionFunctions.Add(functionInfo.Name, functionInfo);
                }

                LexicalAnalyzer = new LexicalAnalyzer(Expression);
                AllowUnknownKeywords = allowUnknownKeywords;
            }

            private class NoOperationTraceWriter : ITraceWriter
            {
                public void Info(String message)
                {
                }

                public void Verbose(String message)
                {
                }
            }
        }
    }
}
