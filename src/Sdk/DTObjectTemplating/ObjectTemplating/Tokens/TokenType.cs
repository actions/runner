using System;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    internal static class TokenType
    {
        internal const Int32 String = 0;

        internal const Int32 Sequence = 1;

        internal const Int32 Mapping = 2;

        internal const Int32 BasicExpression = 3;

        internal const Int32 InsertExpression = 4;

        internal const Int32 Boolean = 5;

        internal const Int32 Number = 6;

        internal const Int32 Null = 7;
    }
}
