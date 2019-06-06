using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.CentralizedFeature.Client
{
    [DataContract]
    public class CentralizedFeatureAvailabilityResult : ISecuredObject
    {
        [DataMember]
        public bool Value { get; }

        [DataMember]
        public CentralizedFeatureFlagTargetServices TargetServices { get; }

        // for binary back compat
        public CentralizedFeatureAvailabilityResult(bool value) : this(value, CentralizedFeatureFlagTargetServices.All) { }

        [JsonConstructor]
        public CentralizedFeatureAvailabilityResult(bool value, CentralizedFeatureFlagTargetServices targetServices)
        {
            Value = value;
            TargetServices = targetServices;
        }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => GraphSecurityConstants.NamespaceId;

        int ISecuredObject.RequiredPermissions => GraphSecurityConstants.ReadByPublicIdentifier;

        string ISecuredObject.GetToken() => GraphSecurityConstants.SubjectsToken;
        #endregion
    }
}
