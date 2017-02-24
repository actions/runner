// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Expressions
// Repo 2) vsts-agent repo on GitHub under src/Microsoft.VisualStudio.Services.Agent/DistributedTask.Expressions
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions
{
    public sealed class Parser
    {
        public INode CreateTree(String expression, ITraceWriter trace, IEnumerable<INamedValueInfo> namedValues, IEnumerable<IFunctionInfo> functions)
        {
            var context = new ParseContext(expression, trace, namedValues, functions);
            context.Trace.Info($"Parsing: <{expression}>");
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

                    // Malformed
                    case TokenKind.Unrecognized:
                        throw new ParseException(ParseExceptionKind.UnrecognizedValue, context.Token, context.Expression);

                    // Unexpected
                    case TokenKind.PropertyName:    // PropertyName should never reach here.
                    case TokenKind.StartParameter:  // StartParameter is only expected by HandleFunction.
                    default:
                        throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
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
                    case TokenKind.Number:
                    case TokenKind.Version:
                    case TokenKind.String:
                        context.Trace.Verbose($"{indent}{context.Token.Kind} '{context.Token.ParsedValue}'");
                        break;
                    // Property or unrecognized
                    case TokenKind.PropertyName:
                    case TokenKind.Unrecognized:
                        context.Trace.Verbose($"{indent}{context.Token.Kind} '{context.Token.RawValue}'");
                        break;
                    // Function or punctuation
                    case TokenKind.WellKnownFunction:
                    case TokenKind.ExtensionFunction:
                    case TokenKind.ExtensionNamedValue:
                    case TokenKind.StartIndex:
                    case TokenKind.StartParameter:
                    case TokenKind.EndIndex:
                    case TokenKind.EndParameter:
                    case TokenKind.Separator:
                    case TokenKind.Dereference:
                        context.Trace.Verbose($"{indent}{context.Token.RawValue}");
                        break;
                    default:
                        throw new NotSupportedException($"Unexpected token kind: {context.Token.Kind}");
                }

                return true;
            }

            return false;
        }

        private static void HandleStartIndex(ParseContext context)
        {
            // Validate follows ")", "]", or a property name.
            if (context.LastToken == null ||
                (context.LastToken.Kind != TokenKind.EndParameter && context.LastToken.Kind != TokenKind.EndIndex && context.LastToken.Kind != TokenKind.PropertyName && context.LastToken.Kind != TokenKind.ExtensionNamedValue))
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Wrap the object being indexed into.
            var indexer = new IndexerNode();
            Node obj = null;
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
            // Validate follows ")", "]", or a property name.
            if (context.LastToken == null ||
                (context.LastToken.Kind != TokenKind.EndParameter && context.LastToken.Kind != TokenKind.EndIndex && context.LastToken.Kind != TokenKind.PropertyName && context.LastToken.Kind != TokenKind.ExtensionNamedValue))
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Wrap the object being indexed into.
            var indexer = new IndexerNode();
            Node obj = null;
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

            // Validate a property name follows.
            if (!TryGetNextToken(context))
            {
                throw new ParseException(ParseExceptionKind.ExpectedPropertyName, context.LastToken, context.Expression);
            }

            if (context.Token.Kind != TokenKind.PropertyName)
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Add the property name to the indexer, as a string.
            String propertyName = context.Token.RawValue;
            indexer.AddParameter(new LiteralValueNode(propertyName));
        }

        private static void HandleEndParameter(ParseContext context)
        {
            ContainerInfo container = context.Containers.Count > 0 ? context.Containers.Peek() : null;  // Validate:
            if (container == null ||                                                        // 1) Container is not null
                !(container.Node is FunctionNode) ||                                        // 2) Container is a function
                container.Node.Parameters.Count < GetMinParamCount(context, container.Token) || // 3) Not below min param threshold
                context.LastToken.Kind == TokenKind.Separator)                              // 4) Last token is not a separator
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
                indexer.Parameters.Count != 2)  // 2) Exactly 2 parameters
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            context.Containers.Pop();
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
            Node node;
            switch (context.Token.Kind)
            {
                case TokenKind.ExtensionNamedValue:
                    String name = context.Token.RawValue;
                    node = context.ExtensionNamedValues[name].CreateNode();
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

        private static void HandleFunction(ParseContext context)
        {
            // Validate either A) is first token OR B) follows "," or "[" or "(".
            if (context.LastToken != null &&
                (context.LastToken.Kind != TokenKind.Separator &&
                context.LastToken.Kind != TokenKind.StartIndex &&
                context.LastToken.Kind != TokenKind.StartParameter))
            {
                throw new ParseException(ParseExceptionKind.UnexpectedSymbol, context.Token, context.Expression);
            }

            // Create the node.
            FunctionNode node;
            String name = context.Token.RawValue;
            switch (context.Token.Kind)
            {
                case TokenKind.WellKnownFunction:
                    node = ExpressionConstants.WellKnownFunctions[name].CreateNode();
                    break;
                case TokenKind.ExtensionFunction:
                    node = context.ExtensionFunctions[name].CreateNode();
                    break;
                default:
                    // Should never reach here.
                    throw new NotSupportedException($"Unexpected function token name: '{context.Token.Kind}'");
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
            context.Containers.Push(new ContainerInfo() { Node = node, Token = context.Token });

            // Validate '(' follows.
            if (!TryGetNextToken(context) || context.Token.Kind != TokenKind.StartParameter)
            {
                throw new ParseException(ParseExceptionKind.ExpectedStartParameter, context.LastToken, context.Expression);
            }
        }

        private static int GetMinParamCount(ParseContext context, Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.WellKnownFunction:
                    return ExpressionConstants.WellKnownFunctions[token.RawValue].MinParameters;
                case TokenKind.ExtensionFunction:
                    return context.ExtensionFunctions[token.RawValue].MinParameters;
                default: // Should never reach here.
                    throw new NotSupportedException($"Unexpected token kind '{token.Kind}'. Unable to determine min param count.");
            }
        }

        private static Int32 GetMaxParamCount(ParseContext context, Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.WellKnownFunction:
                    return ExpressionConstants.WellKnownFunctions[token.RawValue].MaxParameters;
                case TokenKind.ExtensionFunction:
                    return context.ExtensionFunctions[token.RawValue].MaxParameters;
                default: // Should never reach here.
                    throw new NotSupportedException($"Unexpected token kind '{token.Kind}'. Unable to determine max param count.");
            }
        }

        private sealed class ContainerInfo
        {
            public ContainerNode Node { get; set; }

            public Token Token { get; set; }
        }

        private sealed class ParseContext
        {
            public readonly Stack<ContainerInfo> Containers = new Stack<ContainerInfo>();
            public readonly String Expression;
            public readonly Dictionary<String, IFunctionInfo> ExtensionFunctions = new Dictionary<String, IFunctionInfo>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<String, INamedValueInfo> ExtensionNamedValues = new Dictionary<String, INamedValueInfo>(StringComparer.OrdinalIgnoreCase);
            public readonly LexicalAnalyzer Lexer;
            public readonly ITraceWriter Trace;
            public Token Token;
            public Token LastToken;
            public Node Root;

            public ParseContext(String expression, ITraceWriter trace, IEnumerable<INamedValueInfo> namedValues, IEnumerable<IFunctionInfo> functions)
            {
                if (trace == null)
                {
                    throw new ArgumentNullException(nameof(trace));
                }

                Expression = expression ?? String.Empty;
                Trace = trace;
                foreach (INamedValueInfo namedValueInfo in (namedValues ?? new INamedValueInfo[0]))
                {
                    ExtensionNamedValues.Add(namedValueInfo.Name, namedValueInfo);
                }

                foreach (IFunctionInfo functionInfo in (functions ?? new IFunctionInfo[0]))
                {
                    ExtensionFunctions.Add(functionInfo.Name, functionInfo);
                }

                Lexer = new LexicalAnalyzer(Expression, trace, namedValues: ExtensionNamedValues.Keys, functions: ExtensionFunctions.Keys);
            }
        }
    }

    public interface INamedValueInfo
    {
        String Name { get; }
        NamedValueNode CreateNode();
    }

    public class NamedValueInfo<T> : INamedValueInfo
        where T : NamedValueNode, new()
    {
        public NamedValueInfo(String name)
        {
            Name = name;
        }

        public String Name { get; }

        public NamedValueNode CreateNode()
        {
            return new T();
        }
    }

    public interface IFunctionInfo
    {
        String Name { get; }
        Int32 MinParameters { get; }
        Int32 MaxParameters { get; }
        FunctionNode CreateNode();
    }

    public class FunctionInfo<T> : IFunctionInfo
        where T : FunctionNode, new()
    {
        public FunctionInfo(String name, Int32 minParameters, Int32 maxParameters)
        {
            Name = name;
            MinParameters = minParameters;
            MaxParameters = maxParameters;
        }

        public String Name { get; }

        public Int32 MinParameters { get; }

        public Int32 MaxParameters { get; }

        public FunctionNode CreateNode()
        {
            return new T();
        }
    }

    public interface ITraceWriter
    {
        void Info(String message);
        void Verbose(String message);
    }

    public sealed class ParseException : Exception
    {
        internal ParseException(ParseExceptionKind kind, Token token, String expression)
        {
            Expression = expression;
            Kind = kind;
            RawToken = token.RawValue;
            TokenIndex = token.Index;
            String description;
            // TODO: LOC
            switch (kind)
            {
                case ParseExceptionKind.ExpectedPropertyName:
                    description = "Expected property name to follow deference operator";
                    break;
                case ParseExceptionKind.ExpectedStartParameter:
                    description = "Expected '(' to follow function";
                    break;
                case ParseExceptionKind.UnclosedFunction:
                    description = "Unclosed function";
                    break;
                case ParseExceptionKind.UnclosedIndexer:
                    description = "Unclosed indexer";
                    break;
                case ParseExceptionKind.UnexpectedSymbol:
                    description = "Unexpected symbol";
                    break;
                case ParseExceptionKind.UnrecognizedValue:
                    description = "Unrecognized value";
                    break;
                default: // Should never reach here.
                    throw new Exception($"Unexpected parse exception kind '{kind}'.");
            }

            Int32 position = token.Index + 1;
            // TODO: loc
            Message = $"{description}: '{RawToken}'. Located at position {position} within condition expression: {Expression}";
        }

        internal String Expression { get; }

        internal ParseExceptionKind Kind { get; }

        internal String RawToken { get; }

        internal Int32 TokenIndex { get; }

        public sealed override String Message { get; }
    }

    internal enum ParseExceptionKind
    {
        ExpectedPropertyName,
        ExpectedStartParameter,
        UnclosedFunction,
        UnclosedIndexer,
        UnexpectedSymbol,
        UnrecognizedValue,
    }
}