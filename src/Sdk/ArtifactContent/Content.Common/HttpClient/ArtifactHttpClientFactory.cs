using GitHub.Services.Common;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.Identity.Client;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Retain and apply configuration to create derivations of VssHttpClientBase including credentials, timeouts, retry options, and tracing.
    /// Also verifies client connection authorization.
    /// </summary>
    public class ArtifactHttpClientFactory
    {
        public ArtifactHttpClientFactory(
            VssCredentials credentials,
            TimeSpan? httpSendTimeout,
            IAppTraceSource tracer,
            CancellationToken verifyConnectionCancellationToken) : this(tracer, credentials, httpSendTimeout, verifyConnectionCancellationToken)
        {
            ArgumentUtility.CheckForNull(tracer, nameof(tracer));
            ArgumentUtility.CheckForNull(credentials, nameof(credentials));
        }

        /// <summary>
        /// Root and Unit Test constructor
        /// </summary>
        internal ArtifactHttpClientFactory(IAppTraceSource tracer = null, VssCredentials credentials = null, TimeSpan? httpSendTimeout = null, CancellationToken verifyConnectionCancellationToken = default(CancellationToken), VssHttpRetryOptions options = null)
        {
            this.Credentials = credentials;
            this.Tracer = tracer ?? NoopAppTraceSource.Instance;

            this.ClientSettings = VssClientHttpRequestSettings.Default.Clone();
            this.ClientSettings.ClientCertificateManager = null;

#if DEBUG
            if (Debugger.IsAttached)
            {
                Debugger.Break(); // Inform developer we're about to alter the default SendTimeout
                httpSendTimeout = TimeSpan.FromHours(1);
            }
#endif

            if (httpSendTimeout.HasValue)
            {
                this.ClientSettings.SendTimeout = httpSendTimeout.Value;
            }
            else
            {
                // noop, leave default value
            }

            this.DelegatingHandlerFactoryMethods = new List<Func<DelegatingHandler>>();
            this.verifyConnectionCancellationToken = verifyConnectionCancellationToken;

            // Added the below retry HTTP statuscodes to make the logic consistent with the retry options that occur due to exceptions.
            // Tested the same with Fiddler using auto-responder by simulating the below statuscode responses and observed that retry does occur as expected.
            if (options == null)
            {
                options = ArtifactHttpRetryMessageHandler.DefaultRetryOptions;
            }
            else
            {
                options.RetryableStatusCodes.AddRange(ArtifactHttpRetryMessageHandler.DefaultRetryOptions.RetryableStatusCodes);
            }
            this.Options = options;
        }

        public VssCredentials Credentials { get; private set; }

        public VssClientHttpRequestSettings ClientSettings { get; private set; }

        public VssHttpRequestSettings RequestSettings { get { return this.ClientSettings; } }

        protected IList<Func<DelegatingHandler>> DelegatingHandlerFactoryMethods { get; set; }

        protected IAppTraceSource Tracer { get; private set; }

        private readonly CancellationToken verifyConnectionCancellationToken;

        private readonly VssHttpRetryOptions Options;

        /// <summary>
        /// For client types known at designtime
        /// </summary>
        public RequiredInterface CreateVssHttpClient<RequiredInterface, PreferredConcrete>(Uri baseUri)
            where RequiredInterface : IArtifactHttpClient
            where PreferredConcrete : VssHttpClientBase, RequiredInterface
        {
            Tracer.Verbose($"{nameof(ArtifactHttpClientFactory)}.{nameof(CreateVssHttpClient)}: {typeof(PreferredConcrete).Name} with BaseUri: {baseUri}, MaxRetries:{ArtifactHttpRetryMessageHandler.DefaultRetryOptions.MaxRetries}, SendTimeout:{RequestSettings.SendTimeout}");
            return (RequiredInterface)CreateVssHttpClient(typeof(RequiredInterface), typeof(PreferredConcrete), baseUri);
        }

        /// <summary>
        /// Unit tests only.
        /// </summary>
        internal object CreateVssHttpClient(Type requiredInterface, Type preferredType, Uri baseUri, ArtifactHttpRetryMessageHandler retryHandler = null)
        {
            ArgumentUtility.CheckForNull(requiredInterface, nameof(requiredInterface));
            ArgumentUtility.CheckForNull(preferredType, nameof(preferredType));
            ArgumentUtility.CheckForNull(baseUri, nameof(baseUri));

            if (!typeof(IArtifactHttpClient).IsAssignableFrom(requiredInterface))
            {
                throw new ArgumentException($"Type {typeof(IArtifactHttpClient)} is not assignable from {requiredInterface.Name}");
            }

            if (!requiredInterface.IsAssignableFrom(preferredType))
            {
                throw new ArgumentException($"Type {requiredInterface} is not assignable from {preferredType}");
            }

            var paramTypes = new[] { typeof(Uri), typeof(VssCredentials), typeof(VssHttpRequestSettings), typeof(DelegatingHandler[]) };
            if (preferredType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, paramTypes, null) == null)
            {
                throw new ArgumentException(
                    $"{preferredType.Name} must be a non-abstract class with a constructor accepting {nameof(Uri)}, {nameof(VssCredentials)}, {nameof(VssHttpRequestSettings)}, {nameof(DelegatingHandler)}[], {nameof(IAppTraceSource)} types in order to use it as parameter 'T'.");
            }

            // Note VSS uses a similar Activator.CreateInstance call during VssRequestContext.GetClient<T>()
            // via the IVssHttpClientProvider property of IVssRequestContext which is implemented 
            // by ClientProvider.CreateClient in Vssf\Sdk\Server\BusinessLogic\ClientProvider.cs
            // Tests use other implementations such as: Vssf\Sdk\Server.L0.Mocks\Framework\Infrastructure\TestClientProvider.cs
            var handlers = CreateDelegatingHandlers(retryHandler);
            var client = Activator.CreateInstance(preferredType, baseUri, this.Credentials, this.RequestSettings, handlers);

            // Set tracer
            var artifactClient = (IArtifactHttpClient)client;
            artifactClient.SetTracer(this.Tracer);

            return client;
        }

        /// <summary>
        /// Internal method created for migration testing purposes.
        /// retryOnNotFoundTestOnly param is set to true only in testing environment as it is the expected behavior
        /// in migration to fail in the first try on 404 error and then succeed in the next retry attempt.
        /// </summary>
        internal virtual VerifyConnectionResult VerifyConnectionInternal(IArtifactHttpClient httpClient, bool retryOnNotFoundTestOnly = false)
        {
            return TaskSafety.SyncResultOnThreadPool(async () =>
                await new AsyncHttpRetryHelper<VerifyConnectionResult>(
                    async () => await VerifyConnectionInternalAsync(httpClient),
                    2,
                    Tracer,
                    continueOnCapturedContext: false,
                    context: nameof(VerifyConnectionAsync),
                    canRetryDelegate: exception =>
                    {
                        return CanRetryOnNotFoundForVssServiceResponseException(retryOnNotFoundTestOnly, exception);
                    }
                ).InvokeAsync(default));
        }

        public static bool CanRetryOnNotFoundForVssServiceResponseException(bool retryOnNotFoundTestOnly, Exception exception)
        {
            return (retryOnNotFoundTestOnly
                && exception is VssServiceResponseException
                && ((VssServiceResponseException)exception).HttpStatusCode.Equals(HttpStatusCode.NotFound));
        }

        /// <summary>
        /// For client types determined at runtime (e.g. blob client based on version)
        /// </summary>
        public virtual object CreateVssHttpClient(Type requiredInterface, Type preferredType, Uri baseUri)
        {
            return CreateVssHttpClient(requiredInterface, preferredType, baseUri, retryHandler: null);
        }

        /// <summary>
        /// Probes the specified client with an OPTIONS request.
        /// If the probe fails with VssUnauthorizedException, then we assume the user's profile may not exist,
        /// and we issue a request to SPS which will result in a profile creation attempt.
        /// We then send a second OPTIONS request.
        /// </summary>
        public virtual VerifyConnectionResult VerifyConnection(IArtifactHttpClient httpClient)
        {
            return TaskSafety.SyncResultOnThreadPool(() => VerifyConnectionAsync(httpClient));
        }

        /// <summary>
        /// Asynchronously probes the specified client with an OPTIONS request.
        /// If the probe fails with VssUnauthorizedException, then we assume the user's profile may not exist,
        /// and we issue a request to SPS which will result in a profile creation attempt.
        /// We then send a second OPTIONS request.
        /// </summary>
        /// <remarks>
        /// NOTE: Do note call this from inside a controller.
        /// </remarks>
        public virtual async Task<VerifyConnectionResult> VerifyConnectionAsync(IArtifactHttpClient httpClient)
        {
            var retryHelper = new AsyncHttpRetryHelper<VerifyConnectionResult>(
                async () => await VerifyConnectionInternalAsync(httpClient),
                2,
                Tracer,
                continueOnCapturedContext: false,
                context: nameof(VerifyConnectionAsync));

            return await retryHelper.InvokeAsync(default);
        }

        private async Task<VerifyConnectionResult> VerifyConnectionInternalAsync(IArtifactHttpClient httpClient)
        {
            VerifyConnectionResult result = VerifyConnectionResult.Uninitialized;
            Tracer.Verbose($"{nameof(ArtifactHttpClientFactory)}.{nameof(VerifyConnectionAsync)}: {httpClient.GetType().Name}.{nameof(httpClient.GetOptionsAsync)} starting");

            try
            {
                await httpClient.GetOptionsAsync(this.verifyConnectionCancellationToken).ConfigureAwait(false);
                result = VerifyConnectionResult.InitialRequestSucceeded;
            }
            catch (VssUnauthorizedException)
            {
                var connection = new VssConnection(SpsProductionUri, this.Credentials, this.ClientSettings);
                await connection.ConnectAsync(this.verifyConnectionCancellationToken).ConfigureAwait(false);
                var identityClient = connection.GetClient<IdentityHttpClient>();
                await identityClient.GetIdentitySelfAsync(null, this.verifyConnectionCancellationToken).ConfigureAwait(false); // creates a valid profile using the aad token

                // Throws if still Unauthorized
                await httpClient.GetOptionsAsync(this.verifyConnectionCancellationToken).ConfigureAwait(false);
                result = VerifyConnectionResult.RequestSucceededAfterProfileCreation;
            }

            Tracer.Verbose($"{nameof(ArtifactHttpClientFactory)}.{nameof(VerifyConnectionAsync)}: {httpClient.GetType().Name}.{nameof(httpClient.GetOptionsAsync)} completed with {result}");
            return result;
        }

        private static readonly Uri SpsProductionUri = new Uri("https://app.vssps.visualstudio.com");

        /// <remarks>
        /// DelegatingHandlers are created for each client because they cannot be shared across HttpClient pipelines.
        /// System.Net.Http.HttpClientFactory.CreatePipeline enforces this by expecting null for the InnerHandler property.
        /// </remarks>
        private DelegatingHandler[] CreateDelegatingHandlers(ArtifactHttpRetryMessageHandler retryHandler = null)
        {
            var handlers = new List<DelegatingHandler>();
            AddRetryHandler(handlers, retryHandler);

            foreach (var factoryMethod in this.DelegatingHandlerFactoryMethods)
            {
                var handler = factoryMethod();
                handlers.Add(handler);
            }

            return handlers.ToArray();
        }

        private void AddRetryHandler(IList<DelegatingHandler> handlers, ArtifactHttpRetryMessageHandler retryHandler = null)
        {
            var hasRetryHandler = handlers.OfType<ArtifactHttpRetryMessageHandler>().Any();
            if (hasRetryHandler)
            {
                throw new InvalidOperationException($"{nameof(handlers)} already contains a {nameof(ArtifactHttpRetryMessageHandler)}");
            }

            if (retryHandler == null)
            {
                retryHandler = new ArtifactHttpRetryMessageHandler(this.Tracer, this.Options);
            }

            handlers.Add(retryHandler);

            if (this.ClientSettings.MaxRetryRequest != this.Options.MaxRetries)
            {
                // Both VssClientHttpRequestSettings and VssHttpRetryOptions have a retry count.
                // So we give VssHttpRetryOptions.MaxRetries precedence if there's a mismatch.
                this.ClientSettings.MaxRetryRequest = this.Options.MaxRetries;
            }
        }

        public enum VerifyConnectionResult
        {
            Uninitialized
            , InitialRequestSucceeded
            , RequestSucceededAfterProfileCreation
            // , RequestStillUnauthorizedAfterProfileCreation // Throw instead of using this value
        }
    }
}
