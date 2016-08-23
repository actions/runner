using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class OsxServiceControlManager : ServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep vsts`
        private const string _svcNamePattern = "vsts.agent.{0}.{1}";
        private const string _svcDisplayPattern = "VSTS Agent ({0}.{1})";
        private const string _plistTemplate = "vsts.agent.plist.template";
        private const string _shTemplate = "darwin.svc.sh.template";
        private const string _svcShName = "svc.sh";

        public override void GenerateScripts(AgentSettings settings)
        {
            Trace.Entering();

            CalculateServiceName(settings, _svcNamePattern, _svcDisplayPattern);
            try
            {
                string svcShPath = Path.Combine(IOUtil.GetRootPath(), _svcShName);

                // TODO: encoding?
                // TODO: Loc strings formatted into MSG_xxx vars in shellscript
                string svcShContent = File.ReadAllText(Path.Combine(IOUtil.GetBinPath(), _shTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{{SvcDescription}}", ServiceDisplayName },
                                              { "{{SvcNameVar}}", ServiceName }
                                          };

                svcShContent = tokensToReplace.Aggregate(
                    svcShContent,
                    (current, item) => current.Replace(item.Key, item.Value));

                //TODO: encoding?
                File.WriteAllText(svcShPath, svcShContent);

                var unixUtil = HostContext.CreateService<IUnixUtil>();
                unixUtil.ChmodAsync("755", svcShPath).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Trace.Error(e);
                throw;
            }
        }

        public override bool ConfigureService(AgentSettings settings, CommandSettings command)
        {
            Trace.Entering();

            throw new NotSupportedException("OSX Configure Service");
        }

        public override void UnconfigureService()
        {
            Trace.Entering();

            throw new NotSupportedException("OSX unconfigure service");
        }

        public override void StartService()
        {
            Trace.Entering();

            throw new NotSupportedException("OSX start service");
        }

        public override void StopService()
        {
            Trace.Entering();

            throw new NotSupportedException("OSX stop service");
        }

        public override bool CheckServiceExists(string serviceName)
        {
            return false;
        }        
    }
}