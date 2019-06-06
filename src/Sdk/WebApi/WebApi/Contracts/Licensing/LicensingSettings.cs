using System.Collections.Generic;

namespace GitHub.Licensing.WebApi
{
    public class LicensingSettings
    {
        public AccessLevel DefaultAccessLevel { get; set; }

        public IList<AccessLevel> AccessLevelOptions { get; set; }
    }
}
