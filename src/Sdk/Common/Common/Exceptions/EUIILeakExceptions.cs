using GitHub.Services.Common.Internal;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Thrown when EUII leak is detected.
    /// </summary>
    [Serializable]
    public class EUIILeakException : VssException
    {
        public EUIILeakException()
            : base()
        {
        }

        public EUIILeakException(String message)
        : base(CommonResources.EUIILeakException(message))
        {
        }

        public EUIILeakException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
