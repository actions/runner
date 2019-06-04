using System.Runtime.Serialization;

namespace GitHub.Services.FeatureAvailability
{
    [DataContract]
    public class FeatureFlag
    {
        public FeatureFlag(string name, string description, string uri, string effectiveState, string explicitState)
        {
            EffectiveState = effectiveState;
            Uri = uri;
            Name = name;
            Description = description;
            ExplicitState = explicitState;
        }

        public FeatureFlag()
        {
        }

        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public string Description { get; private set; }

        [DataMember]
        public string Uri { get; private set; }

        [DataMember]
        public string EffectiveState { get; private set; }

        [DataMember]
        public string ExplicitState { get; private set; }
    }
}
