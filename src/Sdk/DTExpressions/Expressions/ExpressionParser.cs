using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ExpressionParser
    {
        public ExpressionParser(): this(null)
        {
        }

        public ExpressionParser(ExpressionParserOptions options)
        {
            m_parserOptions = options ?? new ExpressionParserOptions();
        }

        public IExpressionNode CreateTree(
            String expression, 
            ITraceWriter trace, 
            IEnumerable<INamedValueInfo> namedValues, 
            IEnumerable<IFunctionInfo> functions)
        {
            var context = new ParseContext(expression, trace, namedValues, functions, allowUnknownKeywords: false, allowKeywordHyphens: m_parserOptions.AllowHyphens);
            context.Trace.Info($"Parsing expression: <{expression}>");
            return CreateTree(context);
        }

        public void ValidateSyntax(
            String expression, 
            ITraceWriter trace)
        {
            var context = new ParseContext(expression, trace, namedValues: null, functions: null, allowUnknownKeywords: true, allowKeywordHyphens: m_parserOptions.AllowHyphens);
            context.Trace.Info($"Validating expression syntax: <{expression}>");
            CreateTree(context);
        }

        private static IExpressionNode CreateTree(ParseContext context)
        {
            while (TryGetNextToken(context))
            {
                switch (context.Token.Kind)
                {
                    // Punctuation
                    case TokenKind.StartIndex:
                        HandleStartIndex(context);
                        break;
                    case TokenKind.EndIndex:
                        HandleEndIndex(context);
                        break;
                    case TokenKind.EndParameter:
                        HandleEndParameter(context);
                        break;
                    case TokenKind.Separator:
                        HandleSeparator(context);
                        break;
                    case TokenKind.Dereference:
                        HandleDereference(context);
                        break;
                    case TokenKind.Wildcard:
                        HandleWildcard(context);
                        break;

                    // Functions
                    case TokenKind.WellKnownFunction:
                    case TokenKind.ExtensionFunction:
                        HandleFunction(context);
                        break;

                    // Leaf values
                    case TokenKind.Boolean:
                    case TokenKind.Number:
                    case TokenKind.Version:
                    case TokenKind.String:
                    case TokenKind.ExtensionNamedValue:
                        HandleValue(context);
                        break;

                    // Unknown keyword
                    case TokenKind.UnknownKeyword:
                        HandleUnknownKeyword(context);
                        break;

                    // Malformed
                    case TokenKind.Unrecognized:
                        throw new ParseException(ParseExceptionKind.UnrecognizedValue, context.Token, context.Expression);

                    // Unexpected
                    case TokenKind.PropertyName:    // PropertyName should never reach here (HandleDereference reads next token).
                    case TokenKind.StartParameter:  // StartParameter is only expected by HandleFunction.
                    default:
                        throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
                }

                // Validate depth.
                if (context.Containers.Count >= ExpressionConstants.MaxDepth)
                {
                    throw new ParseException(ParseExceptionKind.ExceededMaxDepth, token: null, expression: context.Expression);
                }
            }

            // Validate all containers were closed.
            if (context.Containers.Count > 0)
            {
                ContainerInfo container = context.Containers.Peek();
                if (container.Node is FunctionNode)
                {
                    throw new ParseException(ParseExceptionKind.UnclosedFunction, container.Token, context.Expression);
                }
                else
                {
                    throw new ParseException(ParseExceptionKind.UnclosedIndexer, container.Token, context.Expression);
                }
            }

            return context.Root;
        }

        private static bool TryGetNextToken(ParseContext context)
        {
            context.LastToken = context.Token;
            if (context.Lexer.TryGetNextToken(ref context.Token))
            {
                // Adjust indent level.
                int indentLevel = context.Containers.Count;
                if (indentLevel > 0)
                {
                    switch (context.Token.Kind)
                    {
                        case TokenKind.StartParameter:
                        case TokenKind.EndParameter:
                        case TokenKind.EndIndex:
                            indentLevel--;
                            break;
                    }
                }

                String indent = String.Empty.PadRight(indentLevel * 2, '.');
                switch (context.Token.Kind)
                {
                    // Literal values
                    case TokenKind.Boolean:
                        context.Trace.Verbose($"{indent}{ExpressionUtil.FormatValue(null, context.Token.ParsedValue, ValueKind.Boolean)}");
                        break;
                    case TokenKind.Number:
                        context.Trace.Verbose($"{indent}{ExpressionUtil.FormatValue(null, context.Token.ParsedValue, ValueKind.Number)}");
                        break;
                    case TokenKind.Version:
                        context.Trace.Verbose($"{indent}{ExpressionUtil.FormatValue(null, context.Token.ParsedValue, ValueKind.Version)}");
                        break;
                    case TokenKind.String:
                        context.Trace.Verbose($"{indent}{ExpressionUtil.FormatValue(null, context.Token.ParsedValue, ValueKind.String)}");
                        break;
                    // Property or unrecognized
                    case TokenKind.PropertyName:
                    case TokenKind.Unrecognized:
                        context.Trace.Verbose($"{indent}{context.Token.Kind} {ExpressionUtil.FormatValue(null, context.Token.RawValue, ValueKind.String)}");
                        break;
                    // Function or punctuation
                    case TokenKind.WellKnownFunction:
                    case TokenKind.ExtensionFunction:
                    case TokenKind.ExtensionNamedValue:
                    case TokenKind.Wildcard:
                    case TokenKind.UnknownKeyword:
                    case TokenKind.StartIndex:
                    case TokenKind.StartParameter:
                    case TokenKind.EndIndex:
                    case TokenKind.EndParameter:
                    case TokenKind.Separator:
                    case TokenKind.Dereference:
                        context.Trace.Verbose($"{indent}{context.Token.RawValue}");
                        break;
                    default: // Should never reach here.
                        throw new NotSupportedException($"Unexpected token kind: {context.Token.Kind}");
                }

                return true;
            }

            return false;
        }

        private static void HandleStartIndex(ParseContext context)
        {
            // Validate follows ")", "]", "*", or a property name.
            if (context.LastToken == null ||
                (context.LastToken.Kind != TokenKind.EndParameter && context.LastToken.Kind != TokenKind.EndIndex && context.LastToken.Kind != TokenKind.PropertyName && context.LastToken.Kind != TokenKind.ExtensionNamedValue && context.LastToken.Kind != TokenKind.UnknownKeyword && context.LastToken.Kind != TokenKind.Wildcard))
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Wrap the object being indexed into.
            var indexer = new IndexerNode();
            ExpressionNode obj = null;
            if (context.Containers.Count > 0)
            {
                ContainerNode container = context.Containers.Peek().Node;
                Int32 objIndex = container.Parameters.Count - 1;
                obj = container.Parameters[objIndex];
                container.ReplaceParameter(objIndex, indexer);
            }
            else
            {
                obj = context.Root;
                context.Root = indexer;
            }

            indexer.AddParameter(obj);

            // Update the container stack.
            context.Containers.Push(new ContainerInfo() { Node = indexer, Token = context.Token });
        }

        private static void HandleDereference(ParseContext context)
        {
            // Validate follows ")", "]", "*", or a property name.
            if (context.LastToken == null ||
                (context.LastToken.Kind != TokenKind.EndParameter && context.LastToken.Kind != TokenKind.EndIndex && context.LastToken.Kind != TokenKind.PropertyName && context.LastToken.Kind != TokenKind.ExtensionNamedValue && context.LastToken.Kind != TokenKind.UnknownKeyword && context.LastToken.Kind != TokenKind.Wildcard))
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Wrap the object being indexed into.
            var indexer = new IndexerNode();
            ExpressionNode obj = null;
            if (context.Containers.Count > 0)
            {
                ContainerNode container = context.Containers.Peek().Node;
                Int32 objIndex = container.Parameters.Count - 1;
                obj = container.Parameters[objIndex];
                container.ReplaceParameter(objIndex, indexer);
            }
            else
            {
                obj = context.Root;
                context.Root = indexer;
            }

            indexer.AddParameter(obj);

            // Validate a token follows.
            if (!TryGetNextToken(context))
            {
                throw new ParseException(ParseExceptionKind.ExpectedPropertyName, context.LastToken, context.Expression);
            }

            if (context.Token.Kind == TokenKind.PropertyName)
            {
                indexer.AddParameter(new LiteralValueNode(context.Token.RawValue));
            }
            else if (context.Token.Kind == TokenKind.Wildcard)
            {
                // For a wildcard we add a third parameter, a boolean set to true, so that we know it's a wildcard.
                indexer.AddParameter(new LiteralValueNode(context.Token.RawValue));
                indexer.AddParameter(new LiteralValueNode(true));
            }
            else
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }
        }

        private static void HandleWildcard(ParseContext context)
        {
            // Validate follows "[".
            if (context.LastToken == null ||
                context.LastToken.Kind != TokenKind.StartIndex)
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // When we have a wildcard, we add the wildcard and also third boolean parameter set to true.
            // This lets us differentiate downstream from '*'.
            context.Containers.Peek().Node.AddParameter(new LiteralValueNode(context.Token.RawValue));
            context.Containers.Peek().Node.AddParameter(new LiteralValueNode(true));
        }

        private static void HandleEndParameter(ParseContext context)
        {
            ContainerInfo container = context.Containers.Count > 0 ? context.Containers.Peek() : null;  // Validate:
            if (container == null ||                                                            // 1) Container is not null
                !(container.Node is FunctionNode) ||                                            // 2) Container is a function
                container.Node.Parameters.Count < GetMinParamCount(context, container.Token) || // 3) Not below min param threshold
                container.Node.Parameters.Count > GetMaxParamCount(context, container.Token) || // 4) Not above max param threshold
                context.LastToken.Kind == TokenKind.Separator)                                  // 5) Last token is not a separator
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            context.Containers.Pop();
        }

        private static void HandleEndIndex(ParseContext context)
        {
            IndexerNode indexer = context.Containers.Count > 0 ? context.Containers.Peek().Node as IndexerNode : null;
            //                                  // Validate:
            if (indexer == null ||              // 1) Container is an indexer
                !(indexer.Parameters.Count == 2 || indexer.Parameters.Count == 3))  // 2) Can be 2 or 3 parameters. It's 3 parameters when we are using a filtered array since we 
                                                                                    // set a boolean along with the wildcard.
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            context.Containers.Pop();
        }

        private static void HandleUnknownKeyword(ParseContext context)
        {
            // Validate.
            if (!context.AllowUnknownKeywords)
            {
                throw new ParseException(ParseExceptionKind.UnrecognizedValue, context.Token, context.Expression);
            }

            // Try handle function.
            if (HandleFunction(context, bestEffort: true))
            {
                return;
            }

            // Handle named value.
            HandleValue(context);
        }

        private static void HandleValue(ParseContext context)
        {
            // Validate either A) is the first token OR B) follows "[" "(" or ",".
            if (context.LastToken != null &&
                context.LastToken.Kind != TokenKind.StartIndex &&
                context.LastToken.Kind != TokenKind.StartParameter &&
                context.LastToken.Kind != TokenKind.Separator)
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Create the node.
            ExpressionNode node;
            switch (context.Token.Kind)
            {
                case TokenKind.ExtensionNamedValue:
                    String name = context.Token.RawValue;
                    node = context.ExtensionNamedValues[name].CreateNode();
                    node.Name = name;
                    break;
                case TokenKind.UnknownKeyword:
                    node = new UnknownNamedValueNode();
                    node.Name = context.Token.RawValue;
                    break;
                default:
                    node = new LiteralValueNode(context.Token.ParsedValue);
                    break;
            }

            // Update the tree.
            if (context.Root == null)
            {
                context.Root = node;
            }
            else
            {
                context.Containers.Peek().Node.AddParameter(node);
            }
        }

        private static void HandleSeparator(ParseContext context)
        {
            ContainerInfo container = context.Containers.Count > 0 ? context.Containers.Peek() : null;  // Validate:
            if (container == null ||                                                            // 1) Container is not null
                !(container.Node is FunctionNode) ||                                            // 2) Container is a function
                container.Node.Parameters.Count < 1 ||                                          // 3) At least one parameter
                container.Node.Parameters.Count >= GetMaxParamCount(context, container.Token) ||// 4) Under max parameters threshold
                context.LastToken.Kind == TokenKind.Separator)                                  // 5) Last token is not a separator
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }
        }

        private static Boolean HandleFunction(
            ParseContext context,
            Boolean bestEffort = false)
        {
            // Validate either A) is first token OR B) follows "," or "[" or "(".
            if (context.LastToken != null &&
                (context.LastToken.Kind != TokenKind.Separator &&
                context.LastToken.Kind != TokenKind.StartIndex &&
                context.LastToken.Kind != TokenKind.StartParameter))
            {
                if (bestEffort)
                {
                    return false;
                }

                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Validate '(' follows.
            if (bestEffort)
            {
                Token nextToken = null;
                if (!context.Lexer.TryPeekNextToken(ref nextToken) || nextToken.Kind != TokenKind.StartParameter)
                {
                    return false;
                }

                TryGetNextToken(context);
            }
            else if (!TryGetNextToken(context) || context.Token.Kind != TokenKind.StartParameter)
            {
                throw new ParseException(ParseExceptionKind.ExpectedStartParameter, context.LastToken, context.Expression);
            }

            // Create the node.
            FunctionNode node;
            String name = context.LastToken.RawValue;
            switch (context.LastToken.Kind)
            {
                case TokenKind.WellKnownFunction:
                    node = ExpressionConstants.WellKnownFunctions[name].CreateNode();
                    node.Name = name;
                    break;
                case TokenKind.ExtensionFunction:
                    node = context.ExtensionFunctions[name].CreateNode();
                    node.Name = name;
                    break;
                case TokenKind.UnknownKeyword:
                    node = new UnknownFunctionNode();
                    node.Name = name;
                    break;
                default:
                    // Should never reach here.
                    throw new NotSupportedException($"Unexpected function token name: '{context.LastToken.Kind}'");
            }

            // Update the tree.
            if (context.Root == null)
            {
                context.Root = node;
            }
            else
            {
                context.Containers.Peek().Node.AddParameter(node);
            }

            // Update the container stack.
            context.Containers.Push(new ContainerInfo() { Node = node, Token = context.LastToken });
            return true;
        }

        private static int GetMinParamCount(
            ParseContext context,
            Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.WellKnownFunction:
                    return ExpressionConstants.WellKnownFunctions[token.RawValue].MinParameters;
                case TokenKind.ExtensionFunction:
                    return context.ExtensionFunctions[token.RawValue].MinParameters;
                case TokenKind.UnknownKeyword:
                    return 0;
                default: // Should never reach here.
                    throw new NotSupportedException($"Unexpected token kind '{token.Kind}'. Unable to determine min param count.");
            }
        }

        private static Int32 GetMaxParamCount(
            ParseContext context,
            Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.WellKnownFunction:
                    return ExpressionConstants.WellKnownFunctions[token.RawValue].MaxParameters;
                case TokenKind.ExtensionFunction:
                    return context.ExtensionFunctions[token.RawValue].MaxParameters;
                case TokenKind.UnknownKeyword:
                    return Int32.MaxValue;
                default: // Should never reach here.
                    throw new NotSupportedException($"Unexpected token kind '{token.Kind}'. Unable to determine max param count.");
            }
        }

        private ExpressionParserOptions m_parserOptions;

        private sealed class ContainerInfo
        {
            public ContainerNode Node { get; set; }

            public Token Token { get; set; }
        }

        private sealed class ParseContext
        {
            public readonly Boolean AllowUnknownKeywords;
            public readonly Stack<ContainerInfo> Containers = new Stack<ContainerInfo>();
            public readonly String Expression;
            public readonly Dictionary<String, IFunctionInfo> ExtensionFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<String, INamedValueInfo> ExtensionNamedValues = new Dictionary<String, INamedValueInfo>(StringComparer.OrdinalIgnoreCase);
            public readonly LexicalAnalyzer Lexer;
            public readonly ITraceWriter Trace;
            public Token Token;
            public Token LastToken;
            public ExpressionNode Root;

            public ParseContext(
                String expression,
                ITraceWriter trace,
                IEnumerable<INamedValueInfo> namedValues,
                IEnumerable<IFunctionInfo> functions,
                Boolean allowUnknownKeywords = false,
                Boolean allowKeywordHyphens = false)
            {
                Expression = expression ?? String.Empty;
                if (Expression.Length > ExpressionConstants.MaxLength)
                {
                    throw new ParseException(ParseExceptionKind.ExceededMaxLength, token: null, expression: Expression);
                }

                Trace = trace ?? new NoOperationTraceWriter();
                foreach (INamedValueInfo namedValueInfo in (namedValues ?? new INamedValueInfo[0]))
                {
                    ExtensionNamedValues.Add(namedValueInfo.Name, namedValueInfo);
                }

                foreach (IFunctionInfo functionInfo in (functions ?? new IFunctionInfo[0]))
                {
                    ExtensionFunctions.Add(functionInfo.Name, functionInfo);
                }

                AllowUnknownKeywords = allowUnknownKeywords;
                Lexer = new LexicalAnalyzer(Expression, namedValues: ExtensionNamedValues.Keys, functions: ExtensionFunctions.Keys, allowKeywordHyphens);
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
