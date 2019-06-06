using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// Provides data necessary for authorizing the connecter server using OAuth 2.0 authentication flows.
    /// </summary>
    [DataContract]
    public sealed class ConnectedServerAuthorization
    {
        /// <summary>
        /// Initializes a new <c>ConnectedSeverAuthorization</c> instance with default values.
        /// </summary>
        public ConnectedServerAuthorization()
        {
        }

        private ConnectedServerAuthorization(ConnectedServerAuthorization objectToBeCloned)
        {
            this.AuthorizationUrl = objectToBeCloned.AuthorizationUrl;
            this.ClientId = objectToBeCloned.ClientId;
            this.PublicKey = objectToBeCloned.PublicKey;
        }

        /// <summary>
        /// Gets or sets the endpoint used to obtain access tokens from the configured token service.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri AuthorizationUrl
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the client identifier for this agent.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid ClientId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the public key used to verify the identity of this connected server.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String PublicKey
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a deep copy of the authorization data.
        /// </summary>
        /// <returns>A new <c>ConnectedServerAuthorization</c> instance copied from the current instance</returns>
        public ConnectedServerAuthorization Clone()
        {
            return new ConnectedServerAuthorization(this);
        }
    }
}
