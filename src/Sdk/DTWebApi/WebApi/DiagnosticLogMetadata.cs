using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class DiagnosticLogMetadata
    {
        public DiagnosticLogMetadata(string agentName, int agentId, int poolId, string phaseName, string fileName, string phaseResult)
        {
            AgentName = agentName;
            AgentId = agentId;
            PoolId = poolId;
            PhaseName = phaseName;
            FileName = fileName;
            PhaseResult = phaseResult;
        }

        [DataMember]
        public string AgentName { get; set; }

        [DataMember]
        public int AgentId { get; set; }

        [DataMember]
        public int PoolId { get; set; }

        [DataMember]
        public string PhaseName { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string PhaseResult { get; set; }
    }
}
