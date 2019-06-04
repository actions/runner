﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Capabilities
{
    [ServiceLocator(Default = typeof(CapabilitiesManager))]
    public interface ICapabilitiesManager : IRunnerService
    {
        Task<Dictionary<string, string>> GetCapabilitiesAsync(RunnerSettings settings, CancellationToken token);
    }

    public sealed class CapabilitiesManager : RunnerService, ICapabilitiesManager
    {
        public async Task<Dictionary<string, string>> GetCapabilitiesAsync(RunnerSettings settings, CancellationToken cancellationToken)
        {
            Trace.Entering();
            ArgUtil.NotNull(settings, nameof(settings));

            // Initialize a dictionary of capabilities.
            var capabilities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (settings.SkipCapabilitiesScan)
            {
                Trace.Info("Skip capabilities scan.");
                return capabilities;
            }

            // Get the providers.
            var extensionManager = HostContext.GetService<IExtensionManager>();
            IEnumerable<ICapabilitiesProvider> providers =
                extensionManager
                .GetExtensions<ICapabilitiesProvider>()
                ?.OrderBy(x => x.Order);

            // Add each capability returned from each provider.
            foreach (ICapabilitiesProvider provider in providers ?? new ICapabilitiesProvider[0])
            {
                foreach (Capability capability in await provider.GetCapabilitiesAsync(settings, cancellationToken) ?? new List<Capability>())
                {
                    // Make sure we mask secrets in capabilities values.
                    capabilities[capability.Name] = HostContext.SecretMasker.MaskSecrets(capability.Value);
                }
            }

            return capabilities;
        }
    }

    public interface ICapabilitiesProvider : IExtension
    {
        int Order { get; }

        Task<List<Capability>> GetCapabilitiesAsync(RunnerSettings settings, CancellationToken cancellationToken);
    }

    public sealed class Capability
    {
        public string Name { get; }
        public string Value { get; }

        public Capability(string name, string value)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            Name = name;
            Value = value ?? string.Empty;
        }
    }
}
