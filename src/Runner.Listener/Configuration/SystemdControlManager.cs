#if OS_LINUX
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Configuration
{
    public class SystemDControlManager : ServiceControlManager, ILinuxServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep runner`
        private const string _svcNamePattern = "actions.runner.{0}.{1}.service";
        private const string _svcDisplayPattern = "GitHub Actions Runner ({0}.{1})";
        private const string _shTemplate = "systemd.svc.sh.template";
        private const string _shName = "svc.sh";

        public void GenerateScripts(RunnerSettings settings)
        {
            try
            {
                string serviceName;
                string serviceDisplayName;
                CalculateServiceName(settings, _svcNamePattern, _svcDisplayPattern, out serviceName, out serviceDisplayName);

                string svcShPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), _shName);

                string svcShContent = File.ReadAllText(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), _shTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{{SvcDescription}}", serviceDisplayName },
                                              { "{{SvcNameVar}}", serviceName }
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
    }
}
#endif
