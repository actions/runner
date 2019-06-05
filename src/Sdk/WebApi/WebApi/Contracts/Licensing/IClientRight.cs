using System;

namespace GitHub.Services.Licensing
{
    /// <summary>
    /// Container for client licensing rights
    /// </summary>
    public interface IClientRight : IUsageRight
    {
        string AuthorizedVSEdition { get; }

        Version ClientVersion { get; }

        string LicenseDescriptionId { get; }

        string LicenseFallbackDescription { get; }

        string LicenseUrl { get; }

        string LicenseSourceName { get; }
    }
}
