using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using GitHub.Runner.Common.Util;
using GitHub.Services.Client;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Services.OAuth;

namespace GitHub.Runner.Listener.Configuration
{
    public interface ICredentialProvider
    {
        Boolean RequireInteractive { get; }
        CredentialData CredentialData { get; set; }
        VssCredentials GetVssCredentials(IHostContext context);
        void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl);
    }

    public abstract class CredentialProvider : ICredentialProvider
    {
        public CredentialProvider(string scheme)
        {
            CredentialData = new CredentialData();
            CredentialData.Scheme = scheme;
        }

        public virtual Boolean RequireInteractive => false;
        public CredentialData CredentialData { get; set; }

        public abstract VssCredentials GetVssCredentials(IHostContext context);
        public abstract void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl);
    }

    public sealed class AadDeviceCodeAccessToken : CredentialProvider
    {
        private string _azureDevOpsClientId = "97877f11-0fc6-4aee-b1ff-febb0519dd00";

        public override Boolean RequireInteractive => true;

        public AadDeviceCodeAccessToken() : base(Constants.Configuration.AAD) { }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            Tracing trace = context.GetTrace(nameof(AadDeviceCodeAccessToken));
            trace.Info(nameof(GetVssCredentials));
            ArgUtil.NotNull(CredentialData, nameof(CredentialData));

            CredentialData.Data.TryGetValue(Constants.Runner.CommandLine.Args.Url, out string serverUrl);
            ArgUtil.NotNullOrEmpty(serverUrl, nameof(serverUrl));

            var tenantAuthorityUrl = GetTenantAuthorityUrl(context, serverUrl);
            if (tenantAuthorityUrl == null)
            {
                throw new NotSupportedException($"'{serverUrl}' is not backed by Azure Active Directory.");
            }

            LoggerCallbackHandler.LogCallback = ((LogLevel level, string message, bool containsPii) =>
            {
                switch (level)
                {
                    case LogLevel.Information:
                        trace.Info(message);
                        break;
                    case LogLevel.Error:
                        trace.Error(message);
                        break;
                    case LogLevel.Warning:
                        trace.Warning(message);
                        break;
                    default:
                        trace.Verbose(message);
                        break;
                }
            });

            LoggerCallbackHandler.UseDefaultLogging = false;
            AuthenticationContext ctx = new AuthenticationContext(tenantAuthorityUrl.AbsoluteUri);
            var queryParameters = $"redirect_uri={Uri.EscapeDataString(new Uri(serverUrl).GetLeftPart(UriPartial.Authority))}";
            DeviceCodeResult codeResult = ctx.AcquireDeviceCodeAsync("https://management.core.windows.net/", _azureDevOpsClientId, queryParameters).GetAwaiter().GetResult();

            var term = context.GetService<ITerminal>();
            term.WriteLine($"Please finish AAD device code flow in browser ({codeResult.VerificationUrl}), user code: {codeResult.UserCode}");
            if (string.Equals(CredentialData.Data[Constants.Runner.CommandLine.Flags.LaunchBrowser], bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
#if OS_WINDOWS
                    Process.Start(new ProcessStartInfo() { FileName = codeResult.VerificationUrl, UseShellExecute = true });
#elif OS_LINUX
                    Process.Start(new ProcessStartInfo() { FileName = "xdg-open", Arguments = codeResult.VerificationUrl });
#else
                    Process.Start(new ProcessStartInfo() { FileName = "open", Arguments = codeResult.VerificationUrl });
#endif
                }
                catch (Exception ex)
                {
                    // not able to open browser, ex: xdg-open/open is not installed.
                    trace.Error(ex);
                    term.WriteLine($"Fail to open browser. {codeResult.Message}");
                }
            }

            AuthenticationResult authResult = ctx.AcquireTokenByDeviceCodeAsync(codeResult).GetAwaiter().GetResult();
            ArgUtil.NotNull(authResult, nameof(authResult));
            trace.Info($"receive AAD auth result with {authResult.AccessTokenType} token");

            var aadCred = new VssAadCredential(new VssAadToken(authResult));
            VssCredentials creds = new VssCredentials(null, aadCred, CredentialPromptType.DoNotPrompt);
            trace.Info("cred created");

            return creds;
        }

        public override void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl)
        {
            ArgUtil.NotNull(context, nameof(context));
            Tracing trace = context.GetTrace(nameof(AadDeviceCodeAccessToken));
            trace.Info(nameof(EnsureCredential));
            ArgUtil.NotNull(command, nameof(command));
            CredentialData.Data[Constants.Runner.CommandLine.Args.Url] = serverUrl;
            CredentialData.Data[Constants.Runner.CommandLine.Flags.LaunchBrowser] = command.GetAutoLaunchBrowser().ToString();
        }

        private Uri GetTenantAuthorityUrl(IHostContext context, string serverUrl)
        {
            using (var client = new HttpClient(context.CreateHttpClientHandler()))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.AddRange(VssClientHttpRequestSettings.Default.UserAgent);
                var requestMessage = new HttpRequestMessage(HttpMethod.Head, $"{serverUrl.Trim('/')}/_apis/connectiondata");
                var response = client.SendAsync(requestMessage).GetAwaiter().GetResult();

                // Get the tenant from the Login URL, MSA backed accounts will not return `Bearer` www-authenticate header.
                var bearerResult = response.Headers.WwwAuthenticate.Where(p => p.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (bearerResult != null && bearerResult.Parameter.StartsWith("authorization_uri=", StringComparison.OrdinalIgnoreCase))
                {
                    var authorizationUri = bearerResult.Parameter.Substring("authorization_uri=".Length);
                    if (Uri.TryCreate(authorizationUri, UriKind.Absolute, out Uri aadTenantUrl))
                    {
                        return aadTenantUrl;
                    }
                }

                return null;
            }
        }
    }

    public sealed class OAuthAccessTokenCredential : CredentialProvider
    {
        public OAuthAccessTokenCredential() : base(Constants.Configuration.OAuthAccessToken) { }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            Tracing trace = context.GetTrace(nameof(OAuthAccessTokenCredential));
            trace.Info(nameof(GetVssCredentials));
            ArgUtil.NotNull(CredentialData, nameof(CredentialData));
            string token;
            if (!CredentialData.Data.TryGetValue(Constants.Runner.CommandLine.Args.Token, out token))
            {
                token = null;
            }

            ArgUtil.NotNullOrEmpty(token, nameof(token));

            trace.Info("token retrieved: {0} chars", token.Length);
            VssCredentials creds = new VssCredentials(null, new VssOAuthAccessTokenCredential(token), CredentialPromptType.DoNotPrompt);
            trace.Info("cred created");

            return creds;
        }

        public override void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl)
        {
            ArgUtil.NotNull(context, nameof(context));
            Tracing trace = context.GetTrace(nameof(OAuthAccessTokenCredential));
            trace.Info(nameof(EnsureCredential));
            ArgUtil.NotNull(command, nameof(command));
            CredentialData.Data[Constants.Runner.CommandLine.Args.Token] = command.GetToken();
        }
    }
}
