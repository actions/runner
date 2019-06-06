using System;
using System.Collections.Generic;

namespace GitHub.Services.Licensing
{
    /// <summary>
    /// Container for licensing rights
    /// </summary>
    public interface IUsageRight
    {
        /// <summary>
        /// Rights data
        /// </summary>
        Dictionary<string, object> Attributes { get; }

        /// <summary>
        /// Rights expiration
        /// </summary>
        DateTimeOffset ExpirationDate { get; }

        /// <summary>
        /// Name, uniquely identifying a usage right
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Version
        /// </summary>
        Version Version { get; }
    }
}
