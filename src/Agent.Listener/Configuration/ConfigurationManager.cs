using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    [ServiceLocator(Default = typeof(ConfigurationManager))]
    public interface IConfigurationManager
    {
        Boolean EnsureConfigured(IHostContext hostContext);

        Boolean Configure(IHostContext hostContext, Dictionary<String, String> args, Boolean unAttended);

        Boolean IsConfigured();

        AgentConfiguration GetConfiguration();
    }

    public sealed class ConfigurationManager : IConfigurationManager
    {
        // TODO Localize the description?
        private static Dictionary<string, ArgumentMetaData> argumentMetaData =
            new Dictionary<string, ArgumentMetaData>
                {
                    {
                        "ServerUrl",
                        new ArgumentMetaData
                            {
                                Description = "Server Url" ,
                                Validator = Validators.ServerUrlValidator
                            }
                    },
                    {
                        "PoolName",
                        new ArgumentMetaData
                            {
                                Description = "Pool Name",
                                DefaultValue = "Default",
                                Validator = Validators.NonEmptyValidator
                            }
                    },
                    {
                        //TODO: Get the MachineName using COREFX
                        "AgentName",
                        new ArgumentMetaData
                            {
                                Description = "Agent Name",
                                Validator = Validators.NonEmptyValidator
                            }
                    },
                    {
                        "AuthType",
                        new ArgumentMetaData
                            {
                                Description = "Authentication Type",
                                DefaultValue = AuthScheme.Pat.ToString(),
                                Validator = Validators.AuthSchemeValidator
                            }
                    },
                    {
                        "WorkFolder",
                        new ArgumentMetaData
                            {
                                Description = "Work Folder",
                                DefaultValue = GetDefaultWorkFolder(),
                                Validator = Validators.FilePathValidator
                            }
                    }
                };

        public Boolean EnsureConfigured(IHostContext context)
        {
            return Configure(context, null, true);
        }

        public bool Configure(IHostContext hostContext, Dictionary<String, String> args, Boolean unAttended)
        {
            this.m_trace = hostContext.Trace["ConfigurationManager"];
            this.configuration = new AgentConfiguration();

            this.m_trace.Info("Read agent settings");
            var consoleWizard = hostContext.GetService<IConsoleWizard>();

            // Get Agent settings
            this.configuration.Setting = new AgentSettings
                                             {
                                                 ServerUrl = consoleWizard.GetConfigurationValue(hostContext, "ServerUrl", argumentMetaData, args, unAttended),
                                                 AgentName = consoleWizard.GetConfigurationValue(hostContext, "AgentName", argumentMetaData, args, unAttended),
                                                 PoolName = consoleWizard.GetConfigurationValue(hostContext, "PoolName", argumentMetaData, args, unAttended),
                                                 WorkFolder = consoleWizard.GetConfigurationValue(hostContext, "WorkFolder", argumentMetaData, args, unAttended)
                                             };

            // Get authentication type
            var authType = consoleWizard.GetConfigurationValue(hostContext, "AuthType", argumentMetaData, args, unAttended).ConvertToEnum<AuthScheme>();

            var credentialManager = hostContext.GetService<IAgentCredentialManager>();

            var credential = credentialManager.Create(authType);
            credential.ReadCredential(hostContext, args, unAttended);

            // TODO connect to server if suceeds save the config
            return true;
        }

        public bool IsConfigured()
        {
            try
            {
                // TODO: Check if the agent config parameter exists too
                return new FileInfo(GetConfigFile()).Exists;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public AgentConfiguration GetConfiguration()
        {
            return this.configuration;
        }

        private void LoadConfiguration()
        {
            //TODO Load previous stored config
        }

        private static String GetDefaultWorkFolder()
        {
            var rootFolder = GetRootFolder();

            if (!string.IsNullOrEmpty(rootFolder))
            {
                return Path.Combine(rootFolder, "_work");
            }

            return String.Empty;
        }

        private static String GetConfigFile()
        {
            var rootFolder = GetRootFolder();

            if (string.IsNullOrEmpty(rootFolder))
            {
                return Path.Combine(rootFolder, ".Agent");
            }

            return String.Empty;
        }

        private static String GetRootFolder()
        {
            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;

            if (!string.IsNullOrEmpty(currentAssemblyLocation))
            {
                return new DirectoryInfo(currentAssemblyLocation).Parent.FullName.ToString();
            }

            return string.Empty;
        }

        private Dictionary<string, string> LoadedConfig
        {
            get
            {
                if (!this.isConfigLoaded)
                {
                    this.LoadConfiguration();
                }

                return this.existingConfig;
            }
        }

        private TraceSource m_trace;

        private AgentConfiguration configuration;

        private bool isConfigLoaded;

        private Dictionary<string, string> existingConfig;
    }
}