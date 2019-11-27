#if OS_OSX
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Configuration
{
    public class OsxServiceControlManager : ServiceControlManager, ILinuxServiceControlManager
    {
        // This is the name you would see when you do `systemctl list-units | grep runner`
        private const string _svcNamePattern = "actions.runner.{0}.{1}";
        private const string _svcDisplayPattern = "GitHub Actions Runner ({0}.{1})";
        private const string _shTemplate = "darwin.svc.sh.template";
        private const string _svcShName = "svc.sh";

        public void GenerateScripts(RunnerSettings settings)
        {
            Trace.Entering();

            string serviceName;
            string serviceDisplayName;
            CalculateServiceName(settings, _svcNamePattern, _svcDisplayPattern, out serviceName, out serviceDisplayName);

            try
            {
                string svcShPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), _svcShName);

                // TODO: encoding?
                // TODO: Loc strings formatted into MSG_xxx vars in shellscript
                string svcShContent = File.ReadAllText(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Bin), _shTemplate));
                var tokensToReplace = new Dictionary<string, string>
                                          {
                                              { "{{SvcDescription}}", serviceDisplayName },
                                              { "{{SvcNameVar}}", serviceName }
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
    }
}
#endif
