﻿using System;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services.Common;

namespace GitHub.Services.Settings
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class InvalidSettingsScopeException : VssServiceException
    {
        public InvalidSettingsScopeException(String message)
            : base(message)
        {
        }

        public InvalidSettingsScopeException(String message, Exception ex)
            : base(message, ex)
        {
        }
    }
}
