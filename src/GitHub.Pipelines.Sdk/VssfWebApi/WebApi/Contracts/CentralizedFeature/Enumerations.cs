using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.CentralizedFeature
{
    [DataContract]
    public enum CentralizedFeatureFlags
    {
        [EnumMember]
        UserShardingDualWriteToDeployment = 1
    }

    [DataContract]
    public enum CentralizedFeatureFlagTargetServices
    {
        [EnumMember]
        All = 0,

        [EnumMember]
        SpsAndTfs = 1,

        [EnumMember]
        SpsOnly = 2,
    }
}
