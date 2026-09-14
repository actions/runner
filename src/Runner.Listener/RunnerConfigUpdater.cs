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
        Task<RunnerConfigUpdateResult> UpdateRunnerConfigAsync(string runnerQualifiedId, string configType, string serviceType, string configRefreshUrl);
    }

    public enum RunnerConfigUpdateStatus
    {
        Failed,
        NoTarget,
        Updated
    }

    public sealed class RunnerConfigUpdateResult
    {
        private RunnerConfigUpdateResult(RunnerConfigUpdateStatus status, RunnerSettings targetRunnerSettings = null)
        {
            Status = status;
            TargetRunnerSettings = targetRunnerSettings;
        }

        public RunnerConfigUpdateStatus Status { get; }

        public RunnerSettings TargetRunnerSettings { get; }

        public static RunnerConfigUpdateResult Failed()
        {
            return new RunnerConfigUpdateResult(RunnerConfigUpdateStatus.Failed);
        }

        public static RunnerConfigUpdateResult NoTarget()
        {
            return new RunnerConfigUpdateResult(RunnerConfigUpdateStatus.NoTarget);
        }

        public static RunnerConfigUpdateResult Updated(RunnerSettings targetRunnerSettings = null)
        {
            return new RunnerConfigUpdateResult(RunnerConfigUpdateStatus.Updated, targetRunnerSettings);
        }
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

        public async Task<RunnerConfigUpdateResult> UpdateRunnerConfigAsync(string runnerQualifiedId, string configType, string serviceType, string configRefreshUrl)
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
                    return RunnerConfigUpdateResult.Failed();
                }

                // keep the timeout short to avoid blocking the main thread
                using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    switch (configType.ToLowerInvariant())
                    {
                        case "runner":
                            return await UpdateRunnerSettingsAsync(serviceType, configRefreshUrl, tokenSource.Token);
                        case "credentials":
                            return await UpdateRunnerCredentialsAsync(serviceType, configRefreshUrl, tokenSource.Token);
                        default:
                            Trace.Error($"Invalid config type '{configType}'.");
                            await ReportTelemetryAsync($"Invalid config type '{configType}'.");
                            return RunnerConfigUpdateResult.Failed();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Error($"Failed to update runner '{configType}' config.");
                Trace.Error(ex);
                await ReportTelemetryAsync($"Failed to update runner '{configType}' config: {ex}");
                return RunnerConfigUpdateResult.Failed();
            }
        }

        private async Task<RunnerConfigUpdateResult> UpdateRunnerSettingsAsync(string serviceType, string configRefreshUrl, CancellationToken token)
        {
            Trace.Entering();
            // read the current runner settings and encode with base64
            var runnerConfig = HostContext.GetConfigFile(WellKnownConfigFile.Runner);
            string runnerConfigContent = File.ReadAllText(runnerConfig, Encoding.UTF8);
            var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(runnerConfigContent));
            if (string.IsNullOrEmpty(encodedConfig))
            {
                await ReportTelemetryAsync("Failed to get encoded runner settings.");
                return RunnerConfigUpdateResult.Failed();
            }

            // exchange the encoded runner settings with the service
            var refreshedConfig = await RefreshRunnerConfigAsync(encodedConfig, serviceType, "runner", configRefreshUrl, token);
            if (refreshedConfig.Status == RunnerConfigUpdateStatus.Failed)
            {
                return RunnerConfigUpdateResult.Failed();
            }

            if (refreshedConfig.Status == RunnerConfigUpdateStatus.NoTarget)
            {
                // service will return empty string if there is no change in the config
                return RunnerConfigUpdateResult.NoTarget();
            }

            var decodedConfig = Encoding.UTF8.GetString(Convert.FromBase64String(refreshedConfig.EncodedConfig));
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
                return RunnerConfigUpdateResult.Failed();
            }

            // make sure the runner id and name in the refreshed config match the current runner
            if (refreshedRunnerConfig?.AgentId != _settings.AgentId)
            {
                Trace.Error($"Runner id in refreshed config '{refreshedRunnerConfig?.AgentId.ToString() ?? "Empty"}' does not match the current runner '{_settings.AgentId}'.");
                await ReportTelemetryAsync($"Runner id in refreshed config '{refreshedRunnerConfig?.AgentId.ToString() ?? "Empty"}' does not match the current runner '{_settings.AgentId}'.");
                return RunnerConfigUpdateResult.Failed();
            }

            if (refreshedRunnerConfig?.AgentName != _settings.AgentName)
            {
                Trace.Error($"Runner name in refreshed config '{refreshedRunnerConfig?.AgentName ?? "Empty"}' does not match the current runner '{_settings.AgentName}'.");
                await ReportTelemetryAsync($"Runner name in refreshed config '{refreshedRunnerConfig?.AgentName ?? "Empty"}' does not match the current runner '{_settings.AgentName}'.");
                return RunnerConfigUpdateResult.Failed();
            }

            // save the refreshed runner settings as a separate file
            try
            {
                _store.SaveMigratedSettings(refreshedRunnerConfig);
            }
            catch (Exception ex)
            {
                Trace.Error("Failed to save refreshed runner settings.");
                Trace.Error(ex);
                await ReportTelemetryAsync($"Failed to save refreshed runner settings: {ex}");
                return RunnerConfigUpdateResult.Failed();
            }

            await ReportTelemetryAsync("Runner settings updated successfully.");
            return RunnerConfigUpdateResult.Updated(refreshedRunnerConfig);
        }

        private async Task<RunnerConfigUpdateResult> UpdateRunnerCredentialsAsync(string serviceType, string configRefreshUrl, CancellationToken token)
        {
            Trace.Entering();
            // read the current runner credentials and encode with base64
            var credConfig = HostContext.GetConfigFile(WellKnownConfigFile.Credentials);
            string credConfigContent = File.ReadAllText(credConfig, Encoding.UTF8);
            var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(credConfigContent));
            if (string.IsNullOrEmpty(encodedConfig))
            {
                await ReportTelemetryAsync("Failed to get encoded credentials.");
                return RunnerConfigUpdateResult.Failed();
            }

            CredentialData currentCred = _store.GetCredentials();
            if (currentCred == null)
            {
                await ReportTelemetryAsync("Failed to get current credentials.");
                return RunnerConfigUpdateResult.Failed();
            }

            // we only support refreshing OAuth credentials which is used by self-hosted runners.
            if (currentCred.Scheme != Constants.Configuration.OAuth)
            {
                await ReportTelemetryAsync($"Not supported credential scheme '{currentCred.Scheme}'.");
                return RunnerConfigUpdateResult.Failed();
            }

            // exchange the encoded runner credentials with the service
            var refreshedConfig = await RefreshRunnerConfigAsync(encodedConfig, serviceType, "credentials", configRefreshUrl, token);
            if (refreshedConfig.Status == RunnerConfigUpdateStatus.Failed)
            {
                return RunnerConfigUpdateResult.Failed();
            }

            if (refreshedConfig.Status == RunnerConfigUpdateStatus.NoTarget)
            {
                // service will return empty string if there is no change in the config
                return RunnerConfigUpdateResult.NoTarget();
            }

            var decodedConfig = Encoding.UTF8.GetString(Convert.FromBase64String(refreshedConfig.EncodedConfig));
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
                return RunnerConfigUpdateResult.Failed();
            }

            // make sure the credential scheme in the refreshed config match the current credential scheme
            if (refreshedCredConfig?.Scheme != _credData.Scheme)
            {
                Trace.Error($"Credential scheme in refreshed config '{refreshedCredConfig?.Scheme ?? "Empty"}' does not match the current credential scheme '{_credData.Scheme}'.");
                await ReportTelemetryAsync($"Credential scheme in refreshed config '{refreshedCredConfig?.Scheme ?? "Empty"}' does not match the current credential scheme '{_credData.Scheme}'.");
                return RunnerConfigUpdateResult.Failed();
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
                    return RunnerConfigUpdateResult.Failed();
                }

                //  make sure the credential authorizationUrl in the refreshed config match the current credential authorizationUrl for OAuth auth scheme
                var authorizationUrl = _credData.Data.GetValueOrDefault("authorizationUrl", null);
                var refreshedAuthorizationUrl = refreshedCredConfig.Data.GetValueOrDefault("authorizationUrl", null);
                if (authorizationUrl != refreshedAuthorizationUrl)
                {
                    Trace.Error($"Credential authorizationUrl in refreshed config '{refreshedAuthorizationUrl ?? "Empty"}' does not match the current credential authorizationUrl '{authorizationUrl}'.");
                    await ReportTelemetryAsync($"Credential authorizationUrl in refreshed config '{refreshedAuthorizationUrl ?? "Empty"}' does not match the current credential authorizationUrl '{authorizationUrl}'.");
                    return RunnerConfigUpdateResult.Failed();
                }
            }

            // save the refreshed runner credentials as a separate file
            _store.SaveMigratedCredential(refreshedCredConfig);

            if (refreshedCredConfig.Data.ContainsKey("authorizationUrlV2"))
            {
                HostContext.EnableAuthMigration("Credential file updated");
                await ReportTelemetryAsync("Runner credentials updated successfully. Auth migration is enabled.");
            }
            else
            {
                HostContext.DeferAuthMigration(TimeSpan.FromDays(365), "Credential file does not contain authorizationUrlV2");
                await ReportTelemetryAsync("Runner credentials updated successfully. Auth migration is disabled.");
            }

            return RunnerConfigUpdateResult.Updated();
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

        private async Task<RefreshRunnerConfigResult> RefreshRunnerConfigAsync(string encodedConfig, string serviceType, string configType, string configRefreshUrl, CancellationToken token)
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
                        return RefreshRunnerConfigResult.Failed();
                    }
                    break;
                case "runner-admin":
                    throw new NotSupportedException("Runner admin service is not supported.");
                default:
                    Trace.Error($"Invalid service type '{serviceType}'.");
                    await ReportTelemetryAsync($"Invalid service type '{serviceType}'.");
                    return RefreshRunnerConfigResult.Failed();
            }

            if (string.IsNullOrEmpty(refreshedEncodedConfig))
            {
                return RefreshRunnerConfigResult.NoTarget();
            }

            return RefreshRunnerConfigResult.Updated(refreshedEncodedConfig);
        }

        private sealed class RefreshRunnerConfigResult
        {
            private RefreshRunnerConfigResult(RunnerConfigUpdateStatus status, string encodedConfig = null)
            {
                Status = status;
                EncodedConfig = encodedConfig;
            }

            public RunnerConfigUpdateStatus Status { get; }

            public string EncodedConfig { get; }

            public static RefreshRunnerConfigResult Failed()
            {
                return new RefreshRunnerConfigResult(RunnerConfigUpdateStatus.Failed);
            }

            public static RefreshRunnerConfigResult NoTarget()
            {
                return new RefreshRunnerConfigResult(RunnerConfigUpdateStatus.NoTarget);
            }

            public static RefreshRunnerConfigResult Updated(string encodedConfig)
            {
                return new RefreshRunnerConfigResult(RunnerConfigUpdateStatus.Updated, encodedConfig);
            }
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
