using System.Security.Cryptography.X509Certificates;

namespace GitHub.Services.Common
{
    /// <summary>
    /// An interface to allow custom implementations to
    /// gather client certificates when necessary.
    /// </summary>
    public interface IVssClientCertificateManager
    {
        X509Certificate2Collection ClientCertificates { get; }
    }
}
