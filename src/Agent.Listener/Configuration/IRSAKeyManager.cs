using System.Security.Cryptography;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    /// <summary>
    /// Manages an RSA key for the agent using the most appropriate store for the target platform.
    /// </summary>
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(RSACngKeyManager))]
#else
    [ServiceLocator(Default = typeof(RSAFileKeyManager))]
#endif
    public interface IRSAKeyManager : IAgentService
    {
        /// <summary>
        /// Creates a new <c>RSA</c> instance for the current agent. If a key file is found then the current
        /// key is returned to the caller.
        /// </summary>
        /// <returns>An <c>RSA</c> instance representing the key for the agent</returns>
        RSA CreateKey();

        /// <summary>
        /// Deletes the RSA key managed by the key manager.
        /// </summary>
        void DeleteKey();

        /// <summary>
        /// Gets the <c>RSA</c> instance currently stored by the key manager. 
        /// </summary>
        /// <returns>An <c>RSA</c> instance representing the key for the agent</returns>
        /// <exception cref="CryptographicException">No key exists in the store</exception>
        RSA GetKey();
    }
}
