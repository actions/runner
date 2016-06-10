using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.Services.Agent.Util;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class SystemDControlManager : ServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep vsts`
        private const string ServiceNamePattern = "vsts.agent.{0}.{1}.service";
        private const string ServiceDisplayNamePattern = "VSTS Agent ({0}.{1})";

        private const int MaxUserNameLength = 32;
        private const string VstsAgentServiceTemplate = "vsts.agent.service.template";
        private const string _shTemplate = "systemd.svc.sh.template";
        private const string _shName = "svc.sh";

        public override void GenerateScripts(AgentSettings settings)
        {
            try
            {
                CalculateServiceName(settings, ServiceNamePattern, ServiceDisplayNamePattern);
                string svcShPath = Path.Combine(IOUtil.GetRootPath(), _shName);

                string svcShContent = File.ReadAllText(Path.Combine(IOUtil.GetBinPath(), _shTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{{SvcDescription}}", ServiceDisplayName },
                                              { "{{SvcNameVar}}", ServiceName }
                                          };

                svcShContent = tokensToReplace.Aggregate(
                    svcShContent,
                    (current, item) => current.Replace(item.Key, item.Value));

                File.WriteAllText(svcShPath, svcShContent, new UTF8Encoding(false));

                var unixUtil = HostContext.CreateService<IUnixUtil>();
                unixUtil.ChmodAsync("755", svcShPath).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                throw;
            }            
        }

        public override bool ConfigureService(
            AgentSettings settings,
            CommandSettings command)
        {
            Trace.Entering();

            throw new NotSupportedException("Systemd Configure Service");
        }

        public override void UnconfigureService()
        {
            Trace.Entering();

            throw new NotSupportedException("Systemd Unconfigure Service");
        }

        public override void StartService()
        {
            Trace.Entering();

            throw new NotSupportedException("Systemd Start Service");
        }

        public override void StopService()
        {
            Trace.Entering();

            throw new NotSupportedException("Systemd Stop Service");
        }

        public override bool CheckServiceExists(string serviceName)
        {
            return false;
        }
    }
}