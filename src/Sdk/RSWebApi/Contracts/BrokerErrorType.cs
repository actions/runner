using System.Runtime.Serialization;

namespace GitHub.Actions.RunService.WebApi
{
    [DataContract]
    public class BrokerErrorType
    {
        public const string RunnerVersionTooOld = "RunnerVersionTooOld";
    }
}
