using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// The response to an account resource GET request.
    /// </summary>
    public class AccountResource : Resource, IEquatable<AccountResource>
    {
        public AccountResource(string id, string name, string type) : base(id, name, type)
        {
        }

        /// <summary>
        /// Resource properties.
        /// </summary>
        public Dictionary<string, string> properties { get; set; } = new Dictionary<string, string>();

        bool IEquatable<AccountResource>.Equals(AccountResource other)
        {
            return base.Equals(other)
                && properties.Except(other.properties).Count() == 0
                && other.properties.Except(properties).Count() == 0;
        }
    }
}
