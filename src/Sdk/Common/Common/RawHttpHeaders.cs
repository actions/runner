using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.Common.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RawHttpHeaders
    {
        public const String SessionHeader = "X-Runner-Session";
    }
}
