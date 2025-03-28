using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class BrokerErrorKind
    {
        public const string RunnerNotFound = "RunnerNotFound";
        public const string RunnerVersionTooOld = "RunnerVersionTooOld";
        public const string HostedRunnerDeprovisioned = "HostedRunnerDeprovisioned";
    }
}
