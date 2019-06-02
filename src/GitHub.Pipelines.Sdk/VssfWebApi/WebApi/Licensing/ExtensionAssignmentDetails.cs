using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public class ExtensionAssignmentDetails
    {
        public ExtensionAssignmentDetails(ExtensionAssignmentStatus assignmentStatus, string sourceCollectionName = default(string))
        {
            AssignmentStatus = assignmentStatus;
            SourceCollectionName = sourceCollectionName;
        }

        public ExtensionAssignmentStatus AssignmentStatus { get; set; }
        public string SourceCollectionName { get; set; }
    };

    public enum ExtensionAssignmentStatus
    {
        [EnumMember]
        NotEligible = 0,

        [EnumMember]
        NotAssigned = 1,

        [EnumMember]
        AccountAssignment = 2,

        [EnumMember]
        BundleAssignment = 3,

        [EnumMember]
        ImplicitAssignment = 4,

        [EnumMember]
        PendingValidation = 5,

        [EnumMember]
        TrialAssignment = 6,

        [EnumMember]
        RoamingAccountAssignment = 7
    }
}
