using System;
using System.Collections.Generic;

namespace GitHub.Services.Licensing
{
    public interface IClientRightsEnvelope
    {
        Guid ActivityId { get; }

        string Canary { get; }

        DateTimeOffset CreationDate { get; }

        Version EnvelopeVersion { get; }

        DateTimeOffset ExpirationDate { get; }

        TimeSpan RefreshInterval { get; }

        IList<IClientRight> Rights { get; }

        Guid UserId { get; }

        string UserName { get; }
    }
}
