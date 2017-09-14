using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerProvider
{
    /// <summary>
    /// Our factory for VssConnections. Used to ensure:
    ///  1. consistent initialization
    ///  2. Connect() is called but only once for a given Uri.
    /// </summary>
    public static class VssConnectionFactory
    {
        private static readonly ConcurrentDictionary<Uri, VssConnection> _vssConnections = new ConcurrentDictionary<Uri, VssConnection>();

        private static readonly TimeSpan _minTimeout = TimeSpan.FromMinutes(5);

        public static async Task<VssConnection> GetVssConnectionAsync(Uri uri, string accessToken, DelegatingHandler retryOnTimeoutMessageHandler = null)
        {
            VssConnection connection;
            if (!_vssConnections.TryGetValue(uri, out connection))
            {
                VssClientCredentials cred = GetCredentials(accessToken);

                DelegatingHandler[] handlers = new DelegatingHandler[]
                {
                    retryOnTimeoutMessageHandler
                };

                connection = ApiUtil.CreateConnection(uri, cred, handlers);
                connection.Settings.SendTimeout = TimeSpan.FromSeconds(Math.Max(_minTimeout.TotalSeconds, connection.Settings.SendTimeout.TotalSeconds));
                await connection.ConnectAsync().ConfigureAwait(false);

                if (!_vssConnections.TryAdd(uri, connection))
                {
                    // first writer wins. Every caller returned the same instance.
                    connection = _vssConnections[uri];
                }
            }

            return connection;
        }

        private static VssClientCredentials GetCredentials(String accessToken)
        {
            VssClientCredentials cred;
            if (string.IsNullOrEmpty(accessToken))
            {
                cred = new VssClientCredentials(new VssAadCredential());
            }
            else
            {
                cred = new VssClientCredentials(new VssOAuthAccessTokenCredential(accessToken));
            }

            cred.PromptType = CredentialPromptType.DoNotPrompt;
            return cred;
        }
    }
}
