using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.FeatureAvailability
{
    /// <summary>
    /// This is passed to the FeatureFlagController to edit the status of a feature flag
    /// </summary>
    [DataContract]
    public class FeatureFlagPatch
    {
        public FeatureFlagPatch(string state)
        {
            State = state;
        }

        [DataMember]
        public string State { get; private set; }
    }
}
