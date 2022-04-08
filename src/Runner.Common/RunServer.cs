using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Services.WebApi;
using GitHub.Services.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(RunServer))]
    public interface IRunServer : IRunnerService
    {
        Task ConnectAsync(Uri serverUrl, VssCredentials credentials);

        Task<AgentJobRequestMessage> GetJobMessageAsync(Guid scopeId, Guid hostId, string planType, string planGroup, Guid planId, IList<InstanceRef> instanceRefsJson);
    }

    public sealed class RunServer : RunnerService, IRunServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskAgentHttpClient _taskAgentClient;

        public async Task ConnectAsync(Uri serverUrl, VssCredentials credentials)
        {
            // System.Console.WriteLine("RunServer.ConnectAsync");
            _connection = await EstablishVssConnection(serverUrl, credentials, TimeSpan.FromSeconds(100));
            _taskAgentClient = _connection.GetClient<TaskAgentHttpClient>();
            _hasConnection = true;
        }

        private async Task<VssConnection> EstablishVssConnection(Uri serverUrl, VssCredentials credentials, TimeSpan timeout)
        {
            // System.Console.WriteLine("EstablishVssConnection");
            Trace.Info($"EstablishVssConnection");
            Trace.Info($"Establish connection with {timeout.TotalSeconds} seconds timeout.");
            int attemptCount = 5;
            while (attemptCount-- > 0)
            {
                var connection = VssUtil.CreateConnection(serverUrl, credentials, timeout: timeout);
                try
                {
                    await connection.ConnectAsync();
                    return connection;
                }
                catch (Exception ex) when (attemptCount > 0)
                {
                    Trace.Info($"Catch exception during connect. {attemptCount} attempt left.");
                    Trace.Error(ex);

                    await HostContext.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
                }
            }

            // should never reach here.
            throw new InvalidOperationException(nameof(EstablishVssConnection));
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException($"SetConnection");
            }
        }

        public Task<AgentJobRequestMessage> GetJobMessageAsync(
            Guid scopeId,
            Guid hostId,
            string planType,
            string planGroup,
            Guid planId,
            IList<InstanceRef> instanceRefsJson)
        {
            // System.Console.WriteLine("RunServer.GetMessageAsync");
            CheckConnection();
            return _taskAgentClient.GetJobMessageAsync(scopeId, hostId, planType, planGroup, planId, StringUtil.ConvertToJson(instanceRefsJson, Newtonsoft.Json.Formatting.None));
        }
    }

    // todo: move to SDK?
    [DataContract]
    public sealed class MessageRef
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "token")]
        public string Token { get; set; }
        [DataMember(Name = "scopeId")]
        public Guid ScopeId { get; set; }
        [DataMember(Name = "hostId")]
        public Guid HostId { get; set; }
        [DataMember(Name = "planType")]
        public string PlanType { get; set; }
        [DataMember(Name = "planGroup")]
        public string PlanGroup { get; set; }
        [DataMember(Name = "planId")]
        public Guid PlanId { get; set; }
        [DataMember(Name = "instanceRefs")]
        public InstanceRef[] InstanceRefs { get; set; }
        [DataMember(Name = "labels")]
        public string[] Labels { get; set; }
    }

    [DataContract]
    public sealed class InstanceRef
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "instanceType")]
        public string InstanceType { get; set; }
        [DataMember(Name = "attempt")]
        public int Attempt { get; set; }
    }
}
