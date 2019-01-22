using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using System.Xml;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(AzurePowerShellHandler))]
    public interface IAzurePowerShellHandler : IHandler
    {
        AzurePowerShellHandlerData Data { get; set; }
    }

    [ServiceLocator(Default = typeof(PowerShellHandler))]
    public interface IPowerShellHandler : IHandler
    {
        PowerShellHandlerData Data { get; set; }
    }

    public sealed class AzurePowerShellHandler : LegacyPowerShellHandler, IAzurePowerShellHandler
    {
        private const string _connectedServiceName = "ConnectedServiceName";
        private const string _connectedServiceNameSelector = "ConnectedServiceNameSelector";

        public AzurePowerShellHandlerData Data { get; set; }

        protected override void AddLegacyHostEnvironmentVariables(string scriptFile, string workingDirectory)
        {
            // Call the base implementation.
            base.AddLegacyHostEnvironmentVariables(scriptFile: scriptFile, workingDirectory: workingDirectory);

            // additionalStatement
            List<Tuple<String, List<Tuple<String, String>>>> additionalStatement = GetAdditionalCommandsForAzurePowerShell(Inputs);
            if (additionalStatement.Count > 0)
            {
                AddEnvironmentVariable("VSTSPSHOSTSTATEMENTS", JsonUtility.ToString(additionalStatement));
            }
        }

        protected override string GetArgumentFormat()
        {
            ArgUtil.NotNull(Data, nameof(Data));
            return Data.ArgumentFormat;
        }

        protected override string GetTarget()
        {
            ArgUtil.NotNull(Data, nameof(Data));
            return Data.Target;
        }

        protected override string GetWorkingDirectory()
        {
            ArgUtil.NotNull(Data, nameof(Data));
            return Data.WorkingDirectory;
        }

        private List<Tuple<String, List<Tuple<String, String>>>> GetAdditionalCommandsForAzurePowerShell(Dictionary<string, string> inputs)
        {
            List<Tuple<String, List<Tuple<String, String>>>> additionalCommands = new List<Tuple<string, List<Tuple<string, string>>>>();
            string connectedServiceNameValue = GetConnectedService(inputs);

            // It's OK for StorageAccount to not exist (it won't for RunAzurePowerShell or AzureWebPowerShellDeployment)
            // If it is empty for AzureCloudPowerShellDeployment (the UI is set up to require it), the deployment script will
            // fail with a message as to the problem.
            string storageAccountParameter;
            string storageAccount = string.Empty;
            if (inputs.TryGetValue("StorageAccount", out storageAccountParameter))
            {
                storageAccount = storageAccountParameter;
            }

            // Initialize our Azure Support (imports the module, sets up the Azure subscription)
            string path = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "vstshost");
            string azurePSM1 = Path.Combine(path, "Microsoft.TeamFoundation.DistributedTask.Task.Deployment.Azure\\Microsoft.TeamFoundation.DistributedTask.Task.Deployment.Azure.psm1");

            Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - AddCommand(Import-Module)");
            Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - AddParameter(Name={0})", azurePSM1);
            Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - AddParameter(Scope=Global)");
            additionalCommands.Add(new Tuple<string, List<Tuple<string, string>>>("Import-Module",
                                                                                  new List<Tuple<string, string>>()
                                                                                  {
                                                                                      new Tuple<string, string>("Name", azurePSM1),
                                                                                      new Tuple<string, string>("Scope", "Global"),
                                                                                  }));

            Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - AddCommand(Initialize-AzurePowerShellSupport)");
            Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - AddParameter({0}={1})", _connectedServiceName, connectedServiceNameValue);
            Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - AddParameter(StorageAccount={0})", storageAccount);

            additionalCommands.Add(new Tuple<string, List<Tuple<string, string>>>("Initialize-AzurePowerShellSupport",
                                                                                  new List<Tuple<string, string>>()
                                                                                  {
                                                                                                  new Tuple<string, string>(_connectedServiceName, connectedServiceNameValue),
                                                                                                  new Tuple<string, string>("StorageAccount", storageAccount),
                                                                                  }));

            return additionalCommands;
        }

        private string GetConnectedService(Dictionary<string, string> inputs)
        {
            string environment, connectedServiceSelectorValue;
            string connectedServiceName = _connectedServiceName;
            if (inputs.TryGetValue(_connectedServiceNameSelector, out connectedServiceSelectorValue))
            {
                connectedServiceName = connectedServiceSelectorValue;
                Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - Found ConnectedServiceSelector value : {0}", connectedServiceName);
            }

            if (!inputs.TryGetValue(connectedServiceName, out environment))
            {
                Trace.Verbose("AzurePowerShellHandler.UpdatePowerShellEnvironment - Could not find {0}, so looking for DeploymentEnvironmentName.", connectedServiceName);
                if (!inputs.TryGetValue("DeploymentEnvironmentName", out environment))
                {
                    throw new Exception($"The required {connectedServiceName} parameter was not found by the AzurePowerShellRunner.");
                }
            }

            string connectedServiceNameValue = environment;
            if (String.IsNullOrEmpty(connectedServiceNameValue))
            {
                throw new Exception($"The required {connectedServiceName} parameter was either null or empty. Ensure you have provisioned a Deployment Environment using services tab in Admin UI.");
            }

            return connectedServiceNameValue;
        }
    }

    public sealed class PowerShellHandler : LegacyPowerShellHandler, IPowerShellHandler
    {
        public PowerShellHandlerData Data { get; set; }

        protected override string GetArgumentFormat()
        {
            ArgUtil.NotNull(Data, nameof(Data));
            return Data.ArgumentFormat;
        }

        protected override string GetTarget()
        {
            ArgUtil.NotNull(Data, nameof(Data));
            return Data.Target;
        }

        protected override string GetWorkingDirectory()
        {
            ArgUtil.NotNull(Data, nameof(Data));
            return Data.WorkingDirectory;
        }
    }

    public abstract class LegacyPowerShellHandler : Handler
    {
        private Regex _argumentMatching = new Regex("([^\" ]*(\"[^\"]*\")[^\" ]*)|[^\" ]+", RegexOptions.Compiled);
        private string _appConfigFileName = "LegacyVSTSPowerShellHost.exe.config";
        private string _appConfigRestoreFileName = "LegacyVSTSPowerShellHost.exe.config.restore";

        protected abstract string GetArgumentFormat();

        protected abstract string GetTarget();

        protected abstract string GetWorkingDirectory();

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.Directory(TaskDirectory, nameof(TaskDirectory));

            // Resolve the target script.
            string target = GetTarget();
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            string scriptFile = Path.Combine(TaskDirectory, target);
            ArgUtil.File(scriptFile, nameof(scriptFile));

            // Determine the working directory.
            string workingDirectory = GetWorkingDirectory();
            if (String.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Path.GetDirectoryName(scriptFile);
            }
            else
            {
                if (!Directory.Exists(workingDirectory))
                {
                    Directory.CreateDirectory(workingDirectory);
                }
            }

            // Copy the OM binaries into the legacy host folder.
            ExecutionContext.Output(StringUtil.Loc("PrepareTaskExecutionHandler"));
            IOUtil.CopyDirectory(
                source: HostContext.GetDirectory(WellKnownDirectory.ServerOM),
                target: HostContext.GetDirectory(WellKnownDirectory.LegacyPSHost),
                cancellationToken: ExecutionContext.CancellationToken);
            Trace.Info("Finished copying files.");

            // Add the legacy ps host environment variables.
            AddLegacyHostEnvironmentVariables(scriptFile: scriptFile, workingDirectory: workingDirectory);
            AddPrependPathToEnvironment();

            // Add proxy setting to LegacyVSTSPowerShellHost.exe.config
            var agentProxy = HostContext.GetService<IVstsAgentWebProxy>();
            if (!string.IsNullOrEmpty(agentProxy.ProxyAddress))
            {
                AddProxySetting(agentProxy);
            }

            // Invoke the process.
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += OnDataReceived;
                processInvoker.ErrorDataReceived += OnDataReceived;

                try
                {
                    String vstsPSHostExe = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.LegacyPSHost), "LegacyVSTSPowerShellHost.exe");
                    Int32 exitCode = await processInvoker.ExecuteAsync(workingDirectory: workingDirectory,
                                                                       fileName: vstsPSHostExe,
                                                                       arguments: "",
                                                                       environment: Environment,
                                                                       requireExitCodeZero: false,
                                                                       outputEncoding: null,
                                                                       killProcessOnCancel: false,
                                                                       redirectStandardIn: null,
                                                                       inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                                                       cancellationToken: ExecutionContext.CancellationToken);

                    // the exit code from vstsPSHost.exe indicate how many error record we get during execution
                    // -1 exit code means infrastructure failure of Host itself.
                    // this is to match current handler's logic.
                    if (exitCode > 0)
                    {
                        if (ExecutionContext.Result != null)
                        {
                            ExecutionContext.Debug($"Task result already set. Not failing due to error count ({exitCode}).");
                        }
                        else
                        {
                            // We fail task and add issue.
                            ExecutionContext.Result = TaskResult.Failed;
                            ExecutionContext.Error(StringUtil.Loc("PSScriptError", exitCode));
                        }
                    }
                    else if (exitCode < 0)
                    {
                        // We fail task and add issue.
                        ExecutionContext.Result = TaskResult.Failed;
                        ExecutionContext.Error(StringUtil.Loc("VSTSHostNonZeroReturn", exitCode));
                    }
                }
                finally
                {
                    processInvoker.OutputDataReceived -= OnDataReceived;
                    processInvoker.ErrorDataReceived -= OnDataReceived;
                }
            }
        }

        protected virtual void AddLegacyHostEnvironmentVariables(string scriptFile, string workingDirectory)
        {
            // scriptName
            AddEnvironmentVariable("VSTSPSHOSTSCRIPTNAME", scriptFile);

            // workingFolder
            AddEnvironmentVariable("VSTSPSHOSTWORKINGFOLDER", workingDirectory);

            // outputPreference
            AddEnvironmentVariable("VSTSPSHOSTOUTPUTPREFER", ExecutionContext.WriteDebug ? "Continue" : "SilentlyContinue");

            // inputParameters
            if (Inputs.Count > 0)
            {
                AddEnvironmentVariable("VSTSPSHOSTINPUTPARAMETER", JsonUtility.ToString(Inputs));
            }

            List<String> arguments = new List<string>();
            Dictionary<String, String> argumentParameters = new Dictionary<String, String>();
            string argumentFormat = GetArgumentFormat();
            if (string.IsNullOrEmpty(argumentFormat))
            {
                // treatInputsAsArguments
                AddEnvironmentVariable("VSTSPSHOSTINPUTISARG", "True");
            }
            else
            {
                MatchCollection matches = _argumentMatching.Matches(argumentFormat);
                if (matches[0].Value.StartsWith("-"))
                {
                    String currentKey = String.Empty;
                    foreach (Match match in matches)
                    {
                        if (match.Value.StartsWith("-"))
                        {
                            currentKey = match.Value.Trim('-');
                            argumentParameters.Add(currentKey, String.Empty);
                        }
                        else if (!match.Value.StartsWith("-") && !String.IsNullOrEmpty(currentKey))
                        {
                            argumentParameters[currentKey] = match.Value;
                            currentKey = String.Empty;
                        }
                        else
                        {
                            throw new Exception($"Found value {match.Value} with no corresponding named parameter");
                        }
                    }
                }
                else
                {
                    foreach (Match match in matches)
                    {
                        arguments.Add(match.Value);
                    }
                }

                // arguments
                if (arguments.Count > 0)
                {
                    AddEnvironmentVariable("VSTSPSHOSTARGS", JsonUtility.ToString(arguments));
                }

                // argumentParameters
                if (argumentParameters.Count > 0)
                {
                    AddEnvironmentVariable("VSTSPSHOSTARGPARAMETER", JsonUtility.ToString(argumentParameters));
                }
            }

            // push all variable.
            foreach (var variable in ExecutionContext.Variables.Public.Concat(ExecutionContext.Variables.Private))
            {
                AddEnvironmentVariable("VSTSPSHOSTVAR_" + variable.Key, variable.Value);
            }

            // push all public variable.
            foreach (var variable in ExecutionContext.Variables.Public)
            {
                AddEnvironmentVariable("VSTSPSHOSTPUBVAR_" + variable.Key, variable.Value);
            }

            // push all endpoints
            List<String> ids = new List<string>();
            foreach (ServiceEndpoint endpoint in ExecutionContext.Endpoints)
            {
                string partialKey = null;
                if (string.Equals(endpoint.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase))
                {
                    partialKey = WellKnownServiceEndpointNames.SystemVssConnection.ToUpperInvariant();
                    AddEnvironmentVariable("VSTSPSHOSTSYSTEMENDPOINT_URL", endpoint.Url.ToString());
                    AddEnvironmentVariable("VSTSPSHOSTSYSTEMENDPOINT_AUTH", JsonUtility.ToString(endpoint.Authorization));
                }
                else
                {
                    if (endpoint.Id == Guid.Empty && endpoint.Data.ContainsKey("repositoryId"))
                    {
                        partialKey = endpoint.Data["repositoryId"].ToUpperInvariant();
                    }
                    else
                    {
                        partialKey = endpoint.Id.ToString("D").ToUpperInvariant();
                    }

                    ids.Add(partialKey);
                    AddEnvironmentVariable("VSTSPSHOSTENDPOINT_URL_" + partialKey, endpoint.Url.ToString());

                    // We fixed endpoint.name to be the name of the endpoint in yaml, before endpoint.name=endpoint.id is a guid
                    // However, for source endpoint, the endpoint.id is Guid.Empty and endpoint.name is already the name of the endpoint
                    // The legacy PSHost use the Guid to retrive endpoint, the legacy PSHost assume `VSTSPSHOSTENDPOINT_NAME_` is the Guid.
                    if (endpoint.Id == Guid.Empty && endpoint.Data.ContainsKey("repositoryId"))
                    {
                        AddEnvironmentVariable("VSTSPSHOSTENDPOINT_NAME_" + partialKey, endpoint.Name);
                    }
                    else
                    {
                        AddEnvironmentVariable("VSTSPSHOSTENDPOINT_NAME_" + partialKey, endpoint.Id.ToString());
                    }

                    AddEnvironmentVariable("VSTSPSHOSTENDPOINT_TYPE_" + partialKey, endpoint.Type);
                    AddEnvironmentVariable("VSTSPSHOSTENDPOINT_AUTH_" + partialKey, JsonUtility.ToString(endpoint.Authorization));
                    AddEnvironmentVariable("VSTSPSHOSTENDPOINT_DATA_" + partialKey, JsonUtility.ToString(endpoint.Data));
                }
            }

            var defaultRepoName = ExecutionContext.Variables.Get(Constants.Variables.Build.RepoName);
            var defaultRepoType = ExecutionContext.Variables.Get(Constants.Variables.Build.RepoProvider);
            if (!string.IsNullOrEmpty(defaultRepoName))
            {
                // TODO: use alias to find the trigger repo when we have the concept of triggering repo.
                var defaultRepo = ExecutionContext.Repositories.FirstOrDefault(x => String.Equals(x.Properties.Get<string>(RepositoryPropertyNames.Name), defaultRepoName, StringComparison.OrdinalIgnoreCase));
                if (defaultRepo != null && !ids.Exists(x => string.Equals(x, defaultRepo.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    ids.Add(defaultRepo.Id);
                    AddEnvironmentVariable("VSTSPSHOSTENDPOINT_URL_" + defaultRepo.Id, defaultRepo.Url.ToString());
                    AddEnvironmentVariable("VSTSPSHOSTENDPOINT_NAME_" + defaultRepo.Id, defaultRepoName);
                    AddEnvironmentVariable("VSTSPSHOSTENDPOINT_TYPE_" + defaultRepo.Id, defaultRepoType);

                    if (defaultRepo.Endpoint != null)
                    {
                        var endpoint = ExecutionContext.Endpoints.FirstOrDefault(x => x.Id == defaultRepo.Endpoint.Id);
                        if (endpoint != null)
                        {
                            AddEnvironmentVariable("VSTSPSHOSTENDPOINT_AUTH_" + defaultRepo.Id, JsonUtility.ToString(endpoint.Authorization));
                            AddEnvironmentVariable("VSTSPSHOSTENDPOINT_DATA_" + defaultRepo.Id, JsonUtility.ToString(endpoint.Data));
                        }
                    }
                }
            }

            if (ids.Count > 0)
            {
                AddEnvironmentVariable("VSTSPSHOSTENDPOINT_IDS", JsonUtility.ToString(ids));
            }
        }

        private void AddProxySetting(IVstsAgentWebProxy agentProxy)
        {
            string appConfig = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.LegacyPSHost), _appConfigFileName);
            ArgUtil.File(appConfig, _appConfigFileName);

            string appConfigRestore = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.LegacyPSHost), _appConfigRestoreFileName);
            if (!File.Exists(appConfigRestore))
            {
                Trace.Info("Take snapshot of current appconfig for restore modified appconfig.");
                File.Copy(appConfig, appConfigRestore);
            }
            else
            {
                // cleanup any appconfig changes from previous build.
                ExecutionContext.Debug("Restore default LegacyVSTSPowerShellHost.exe.config.");
                IOUtil.DeleteFile(appConfig);
                File.Copy(appConfigRestore, appConfig);
            }

            XmlDocument psHostAppConfig = new XmlDocument();
            using (var appConfigStream = new FileStream(appConfig, FileMode.Open, FileAccess.Read))
            {
                psHostAppConfig.Load(appConfigStream);
            }

            var configuration = psHostAppConfig.SelectSingleNode("configuration");
            ArgUtil.NotNull(configuration, "configuration");

            var exist_defaultProxy = psHostAppConfig.SelectSingleNode("configuration/system.net/defaultProxy");
            if (exist_defaultProxy == null)
            {
                var system_net = psHostAppConfig.SelectSingleNode("configuration/system.net");
                if (system_net == null)
                {
                    Trace.Verbose("Create system.net section in appconfg.");
                    system_net = psHostAppConfig.CreateElement("system.net");
                }

                Trace.Verbose("Create defaultProxy section in appconfg.");
                var defaultProxy = psHostAppConfig.CreateElement("defaultProxy");
                defaultProxy.SetAttribute("useDefaultCredentials", "true");

                Trace.Verbose("Create proxy section in appconfg.");
                var proxy = psHostAppConfig.CreateElement("proxy");
                proxy.SetAttribute("proxyaddress", agentProxy.ProxyAddress);
                proxy.SetAttribute("bypassonlocal", "true");

                if (agentProxy.ProxyBypassList != null && agentProxy.ProxyBypassList.Count > 0)
                {
                    Trace.Verbose("Create bypasslist section in appconfg.");
                    var bypass = psHostAppConfig.CreateElement("bypasslist");
                    foreach (string proxyBypass in agentProxy.ProxyBypassList)
                    {
                        Trace.Verbose($"Create bypasslist.add section for {proxyBypass} in appconfg.");
                        var add = psHostAppConfig.CreateElement("add");
                        add.SetAttribute("address", proxyBypass);
                        bypass.AppendChild(add);
                    }

                    defaultProxy.AppendChild(bypass);
                }

                defaultProxy.AppendChild(proxy);
                system_net.AppendChild(defaultProxy);
                configuration.AppendChild(system_net);

                using (var appConfigStream = new FileStream(appConfig, FileMode.Open, FileAccess.ReadWrite))
                {
                    psHostAppConfig.Save(appConfigStream);
                }

                ExecutionContext.Debug("Add Proxy setting in LegacyVSTSPowerShellHost.exe.config file.");
            }
            else
            {
                //proxy setting exist.
                ExecutionContext.Debug("Proxy setting already exist in LegacyVSTSPowerShellHost.exe.config file.");
            }
        }

        private void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            // This does not need to be inside of a critical section.
            // The logging queues and command handlers are thread-safe.
            if (!CommandManager.TryProcessCommand(ExecutionContext, e.Data))
            {
                ExecutionContext.Output(e.Data);
            }
        }
    }
}
