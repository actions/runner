using System;
using System.Collections.Generic;

namespace GitHub.Services.Licensing
{
    public class ClientRightsEnvelope : IClientRightsEnvelope
    {
        public ClientRightsEnvelope(IList<IClientRight> rights)
        {
            Rights = rights;
        }

        public Guid ActivityId { get; set; }

        public string Canary { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public Version EnvelopeVersion { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }
        
        public TimeSpan RefreshInterval { get; set; }

        public IList<IClientRight> Rights { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; }
    }
}
