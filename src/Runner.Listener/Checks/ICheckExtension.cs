using System.Collections.Generic;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Listener.Check
{
    public interface ICheckExtension : IExtension
    {
        int Order { get; }
        string CheckName { get; }
        string CheckDescription { get; }
        string CheckLog { get; }
        string HelpLink { get; }
        Task<bool> RunCheck(string url, string pat);
    }

    public class CheckResult
    {
        public CheckResult()
        {
            Logs = new List<string>();
        }

        public bool Pass { get; set; }

        public bool SslError { get; set; }

        public List<string> Logs { get; set; }
    }
}
