// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Expressions
// Repo 2) vsts-agent repo on GitHub under src/Microsoft.VisualStudio.Services.Agent/DistributedTask.Expressions
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions
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

    internal enum TokenKind
    {
        // Punctuation
        StartIndex,
        StartParameter,
        EndIndex,
        EndParameter,
        Separator,
        Dereference,

        // Values
        Boolean,
        Number,
        Version,
        String,
        PropertyName,

        // Functions
        WellKnownFunction,
        ExtensionFunction,
        ExtensionNamedValue,

        Unrecognized,
    }
}
