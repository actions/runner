using System.Collections.Generic;

namespace Microsoft.Azure.DevOps.Licensing.WebApi
{
    public class LicensingSettings
    {
        public AccessLevel DefaultAccessLevel { get; set; }

        public IList<AccessLevel> AccessLevelOptions { get; set; }
    }
}
