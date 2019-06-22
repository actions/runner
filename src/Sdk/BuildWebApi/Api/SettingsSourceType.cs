using System;
using System.ComponentModel;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SettingsSourceType
    {
        public const Int32 Definition = 1;
        public const Int32 Process = 2;
    }
}
