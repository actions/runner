using System;

namespace GitHub.DistributedTask.Expressions
{
    internal sealed class Token
    {
        public Token(TokenKind kind, Char rawValue, Int32 index, Object parsedValue = null)
            : this(kind, rawValue.ToString(), index, parsedValue)
        {
        }

        public Token(TokenKind kind, String rawValue, Int32 index, Object parsedValue = null)
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
    }
}
