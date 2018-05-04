using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Sdk
{
    public interface IAgentTaskPlugin
    {
        Guid Id { get; }
        string Version { get; }
        string Stage { get; }
        Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token);
    }

    public class AgentTaskPluginExecutionContext
    {
        private readonly object _stdoutLock = new object();

        public AgentTaskPluginExecutionContext()
        {
            this.Endpoints = new List<ServiceEndpoint>();
            this.Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Repositories = new List<Pipelines.RepositoryResource>();
            this.TaskVariables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            this.Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
        }

        public List<ServiceEndpoint> Endpoints { get; set; }
        public List<Pipelines.RepositoryResource> Repositories { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        public Dictionary<string, VariableValue> TaskVariables { get; set; }
        public Dictionary<string, string> Inputs { get; set; }

        public string GetInput(string name, bool required = false)
        {
            string value = null;
            if (this.Inputs.ContainsKey(name))
            {
                value = this.Inputs[name];
            }

            if (string.IsNullOrEmpty(value) && required)
            {
                throw new ArgumentNullException(name);
            }

            return value;
        }

        public void Error(string message)
        {
            Output($"##vso[task.logissue type=error;]{Escape(message)}");
            Output($"##vso[task.complete result=Failed;]");
        }

        public void Debug(string message)
        {
            Output($"##vso[task.debug]{Escape(message)}");
        }

        public void Warning(string message)
        {
            Output($"##vso[task.logissue type=warning;]{Escape(message)}");
        }

        public void Output(string message)
        {
            lock (_stdoutLock)
            {
                Console.WriteLine(message);
            }
        }

        public void PrependPath(string directory)
        {
            PluginUtil.PrependPath(directory);
            Output($"##vso[task.prependpath]{Escape(directory)}");
        }

        public void Progress(int progress, string operation)
        {
            if (progress < 0 || progress > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(progress));
            }

            Output($"##vso[task.setprogress value={progress}]{Escape(operation)}");
        }

        public void SetSecret(string secret)
        {
            Output($"##vso[task.setsecret]{Escape(secret)}");
        }

        public void SetVariable(string variable, string value, bool isSecret = false)
        {
            this.Variables[variable] = new VariableValue(value, isSecret);
            Output($"##vso[task.setvariable variable={Escape(variable)};issecret={isSecret.ToString()};]{Escape(value)}");
        }

        public void SetTaskVariable(string variable, string value, bool isSecret = false)
        {
            this.TaskVariables[variable] = new VariableValue(value, isSecret);
            Output($"##vso[task.settaskvariable variable={Escape(variable)};issecret={isSecret.ToString()};]{Escape(value)}");
        }

        public void Command(string command)
        {
            Output($"##[command]{Escape(command)}");
        }

        public AgentCertificateSettings GetCertConfiguration()
        {
            bool skipCertValidation = PluginUtil.ConvertToBoolean(this.Variables.GetValueOrDefault("Agent.SkipCertValidation")?.Value);
            string caFile = this.Variables.GetValueOrDefault("Agent.CAInfo")?.Value;
            string clientCertFile = this.Variables.GetValueOrDefault("Agent.ClientCert")?.Value;

            if (!string.IsNullOrEmpty(caFile) || !string.IsNullOrEmpty(clientCertFile) || skipCertValidation)
            {
                var certConfig = new AgentCertificateSettings();
                certConfig.SkipServerCertificateValidation = skipCertValidation;
                certConfig.CACertificateFile = caFile;

                if (!string.IsNullOrEmpty(clientCertFile))
                {
                    certConfig.ClientCertificateFile = clientCertFile;
                    string clientCertKey = this.Variables.GetValueOrDefault("Agent.ClientCertKey")?.Value;
                    string clientCertArchive = this.Variables.GetValueOrDefault("Agent.ClientCertArchive")?.Value;
                    string clientCertPassword = this.Variables.GetValueOrDefault("Agent.ClientCertPassword")?.Value;

                    certConfig.ClientCertificatePrivateKeyFile = clientCertKey;
                    certConfig.ClientCertificateArchiveFile = clientCertArchive;
                    certConfig.ClientCertificatePassword = clientCertPassword;
                }

                return certConfig;
            }
            else
            {
                return null;
            }
        }

        public AgentWebProxySettings GetProxyConfiguration()
        {
            string proxyUrl = this.Variables.GetValueOrDefault("Agent.ProxyUrl")?.Value;
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                string proxyUsername = this.Variables.GetValueOrDefault("Agent.ProxyUsername")?.Value;
                string proxyPassword = this.Variables.GetValueOrDefault("Agent.ProxyPassword")?.Value;
                List<string> proxyBypassHosts = PluginUtil.ConvertFromJson<List<string>>(this.Variables.GetValueOrDefault("Agent.ProxyBypassList")?.Value ?? "[]");
                return new AgentWebProxySettings()
                {
                    ProxyAddress = proxyUrl,
                    ProxyUsername = proxyUsername,
                    ProxyPassword = proxyPassword,
                    ProxyBypassList = proxyBypassHosts,
                };
            }
            else
            {
                return null;
            }
        }

        private string Escape(string input)
        {
            foreach (var mapping in _commandEscapeMappings)
            {
                input = input.Replace(mapping.Key, mapping.Value);
            }

            return input;
        }

        private Dictionary<string, string> _commandEscapeMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {
                ";", "%3B"
            },
            {
                "\r", "%0D"
            },
            {
                "\n", "%0A"
            },
            {
                "]", "%5D"
            },
        };
    }
}
