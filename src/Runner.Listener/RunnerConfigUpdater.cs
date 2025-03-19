using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;

namespace GitHub.Runner.Listener
{
    [ServiceLocator(Default = typeof(RunnerConfigUpdater))]
    public interface IRunnerConfigUpdater : IRunnerService
    {
        Task UpdateRunnerConfigAsync(string runnerQualifiedId, string configType, string serviceType, string configRefreshUrl);
    }

    public sealed class RunnerConfigUpdater : RunnerService, IRunnerConfigUpdater
    {
        private RunnerSettings _settings;
        private CredentialData _credData;
        private IRunnerServer _runnerServer;
        private IConfigurationStore _store;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _store = hostContext.GetService<IConfigurationStore>();
            _settings = _store.GetSettings();
            _credData = _store.GetCredentials();
            _runnerServer = HostContext.GetService<IRunnerServer>();
        }

        public async Task UpdateRunnerConfigAsync(string runnerQualifiedId, string configType, string serviceType, string configRefreshUrl)
        {
            Trace.Entering();
            try
            {
                ArgUtil.NotNullOrEmpty(runnerQualifiedId, nameof(runnerQualifiedId));
                ArgUtil.NotNullOrEmpty(configType, nameof(configType));
                ArgUtil.NotNullOrEmpty(serviceType, nameof(serviceType));
                ArgUtil.NotNullOrEmpty(configRefreshUrl, nameof(configRefreshUrl));

                // make sure the runner qualified id matches the current runner
                if (!await VerifyRunnerQualifiedId(runnerQualifiedId))
                {
                    return;
                }

                // keep the timeout short to avoid blocking the main thread
                using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    switch (configType.ToLowerInvariant())
                    {
                        case "runner":
                            await UpdateRunnerSettingsAsync(serviceType, configRefreshUrl, tokenSource.Token);
                            break;
                        case "credentials":
                            await UpdateRunnerCredentialsAsync(serviceType, configRefreshUrl, tokenSource.Token);
                            break;
                        default:
                            Trace.Error($"Invalid config type '{configType}'.");
                            await ReportTelemetryAsync($"Invalid config type '{configType}'.");
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to update runner '{configType}' config.");
                Trace.Error(ex);
                await ReportTelemetryAsync($"Failed to update runner '{configType}' config: {ex}");
            }
        }

        private async Task UpdateRunnerSettingsAsync(string serviceType, string configRefreshUrl, CancellationToken token)
        {
            Trace.Entering();
            // read the current runner settings and encode with base64
            var runnerConfig = HostContext.GetConfigFile(WellKnownConfigFile.Runner);
            string runnerConfigContent = File.ReadAllText(runnerConfig, Encoding.UTF8);
            var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(runnerConfigContent));
            if (string.IsNullOrEmpty(encodedConfig))
            {
                await ReportTelemetryAsync("Failed to get encoded runner settings.");
                return;
            }

            // exchange the encoded runner settings with the service
            string refreshedEncodedConfig = await RefreshRunnerConfigAsync(encodedConfig, serviceType, "runner", configRefreshUrl, token);
            if (string.IsNullOrEmpty(refreshedEncodedConfig))
            {
                // service will return empty string if there is no change in the config
                return;
            }

            var decodedConfig = Encoding.UTF8.GetString(Convert.FromBase64String(refreshedEncodedConfig));
            RunnerSettings refreshedRunnerConfig;
            try
            {
                refreshedRunnerConfig = StringUtil.ConvertFromJson<RunnerSettings>(decodedConfig);
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to convert runner config from json '{decodedConfig}'.");
                Trace.Error(ex);
                await ReportTelemetryAsync($"Failed to convert runner config '{decodedConfig}' from json: {ex}");
                return;
            }

            // make sure the runner id and name in the refreshed config match the current runner
            if (refreshedRunnerConfig?.AgentId != _settings.AgentId)
            {
                Trace.Error($"Runner id in refreshed config '{refreshedRunnerConfig?.AgentId.ToString() ?? "Empty"}' does not match the current runner '{_settings.AgentId}'.");
                await ReportTelemetryAsync($"Runner id in refreshed config '{refreshedRunnerConfig?.AgentId.ToString() ?? "Empty"}' does not match the current runner '{_settings.AgentId}'.");
                return;
            }

            if (refreshedRunnerConfig?.AgentName != _settings.AgentName)
            {
                Trace.Error($"Runner name in refreshed config '{refreshedRunnerConfig?.AgentName ?? "Empty"}' does not match the current runner '{_settings.AgentName}'.");
                await ReportTelemetryAsync($"Runner name in refreshed config '{refreshedRunnerConfig?.AgentName ?? "Empty"}' does not match the current runner '{_settings.AgentName}'.");
                return;
            }

            // save the refreshed runner settings as a separate file
            _store.SaveMigratedSettings(refreshedRunnerConfig);
            await ReportTelemetryAsync("Runner settings updated successfully.");
        }

        private async Task UpdateRunnerCredentialsAsync(string serviceType, string configRefreshUrl, CancellationToken token)
        {
            Trace.Entering();
            // read the current runner credentials and encode with base64
            var credConfig = HostContext.GetConfigFile(WellKnownConfigFile.Credentials);
            string credConfigContent = File.ReadAllText(credConfig, Encoding.UTF8);
            var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(credConfigContent));
            if (string.IsNullOrEmpty(encodedConfig))
            {
                await ReportTelemetryAsync("Failed to get encoded credentials.");
                return;
            }

            CredentialData currentCred = _store.GetCredentials();
            if (currentCred == null)
            {
                await ReportTelemetryAsync("Failed to get current credentials.");
                return;
            }

            // we only support refreshing OAuth credentials which is used by self-hosted runners.
            if (currentCred.Scheme != Constants.Configuration.OAuth)
            {
                await ReportTelemetryAsync($"Not supported credential scheme '{currentCred.Scheme}'.");
                return;
            }

            // exchange the encoded runner credentials with the service
            string refreshedEncodedConfig = await RefreshRunnerConfigAsync(encodedConfig, serviceType, "credentials", configRefreshUrl, token);
            if (string.IsNullOrEmpty(refreshedEncodedConfig))
            {
                // service will return empty string if there is no change in the config
                return;
            }

            var decodedConfig = Encoding.UTF8.GetString(Convert.FromBase64String(refreshedEncodedConfig));
            CredentialData refreshedCredConfig;
            try
            {
                refreshedCredConfig = StringUtil.ConvertFromJson<CredentialData>(decodedConfig);
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to convert credentials config from json '{decodedConfig}'.");
                Trace.Error(ex);
                await ReportTelemetryAsync($"Failed to convert credentials config '{decodedConfig}' from json: {ex}");
                return;
            }

            // make sure the credential scheme in the refreshed config match the current credential scheme
            if (refreshedCredConfig?.Scheme != _credData.Scheme)
            {
                Trace.Error($"Credential scheme in refreshed config '{refreshedCredConfig?.Scheme ?? "Empty"}' does not match the current credential scheme '{_credData.Scheme}'.");
                await ReportTelemetryAsync($"Credential scheme in refreshed config '{refreshedCredConfig?.Scheme ?? "Empty"}' does not match the current credential scheme '{_credData.Scheme}'.");
                return;
            }

            if (_credData.Scheme == Constants.Configuration.OAuth)
            {
                // make sure the credential clientId in the refreshed config match the current credential clientId for OAuth auth scheme
                var clientId = _credData.Data.GetValueOrDefault("clientId", null);
                var refreshedClientId = refreshedCredConfig.Data.GetValueOrDefault("clientId", null);
                if (clientId != refreshedClientId)
                {
                    Trace.Error($"Credential clientId in refreshed config '{refreshedClientId ?? "Empty"}' does not match the current credential clientId '{clientId}'.");
                    await ReportTelemetryAsync($"Credential clientId in refreshed config '{refreshedClientId ?? "Empty"}' does not match the current credential clientId '{clientId}'.");
                    return;
                }
            }

            // save the refreshed runner credentials as a separate file
            _store.SaveMigratedCredential(refreshedCredConfig);
            await ReportTelemetryAsync("Runner credentials updated successfully.");
        }

        private async Task<bool> VerifyRunnerQualifiedId(string runnerQualifiedId)
        {
            Trace.Entering();
            Trace.Info($"Verifying runner qualified id: {runnerQualifiedId}");
            var idParts = runnerQualifiedId.Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (idParts.Length != 4 || idParts[3] != _settings.AgentId.ToString())
            {
                Trace.Error($"Runner qualified id '{runnerQualifiedId}' does not match the current runner '{_settings.AgentId}'.");
                await ReportTelemetryAsync($"Runner qualified id '{runnerQualifiedId}' does not match the current runner '{_settings.AgentId}'.");
                return false;
            }
            return true;
        }

        private async Task<string> RefreshRunnerConfigAsync(string encodedConfig, string serviceType, string configType, string configRefreshUrl, CancellationToken token)
        {
            string refreshedEncodedConfig;
            switch (serviceType.ToLowerInvariant())
            {
                case "pipelines":
                    try
                    {
                        refreshedEncodedConfig = await _runnerServer.RefreshRunnerConfigAsync((int)_settings.AgentId, configType, encodedConfig, token);
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Failed to refresh runner {configType} config with service.");
                        Trace.Error(ex);
                        await ReportTelemetryAsync($"Failed to refresh {configType} config: {ex}");
                        return null;
                    }
                    break;
                case "runner-admin":
                    throw new NotSupportedException("Runner admin service is not supported.");
                default:
                    Trace.Error($"Invalid service type '{serviceType}'.");
                    await ReportTelemetryAsync($"Invalid service type '{serviceType}'.");
                    return null;
            }

            return refreshedEncodedConfig;
        }

        private async Task ReportTelemetryAsync(string telemetry)
        {
            Trace.Entering();
            try
            {
                using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    await _runnerServer.UpdateAgentUpdateStateAsync(_settings.PoolId, _settings.AgentId, "RefreshConfig", telemetry, tokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                Trace.Error("Failed to report telemetry.");
                Trace.Error(ex);
            }
        }
    }
}
