using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser
{
    /// <summary>
    /// Information about concurrency setting parsed from YML
    /// </summary>
    [DataContract]
    public sealed class GroupPermitSetting
    {
        public GroupPermitSetting(string group) {
            Group = group;
        }

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public bool CancelInProgress { get; set; }
    }
}
