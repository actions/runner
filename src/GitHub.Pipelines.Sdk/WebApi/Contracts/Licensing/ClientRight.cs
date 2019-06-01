using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public class ClientRight : IClientRight
    {
        public Dictionary<string, object> Attributes { get; set; }

        public string AuthorizedVSEdition { get; set; }

        public Version ClientVersion { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        public string LicenseDescriptionId { get; set; }

        public string LicenseFallbackDescription { get; set; }

        public string LicenseUrl { get; set; }

        public string LicenseSourceName { get; set; }

        public string Name { get; set; }

        public Version Version { get; set; }
    }
}
