using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISecretMasker
    {
        void AddRegex(String pattern);
        void AddValue(String value);
        void AddValueEncoder(ValueEncoder encoder);
        ISecretMasker Clone();
        String MaskSecrets(String input);
        public event EventHandler<NewSecretEventArgs> NewSecretAdded;
    }

    public abstract class NewSecretEventArgs : EventArgs
    {
        public abstract String Type { get; }
    }

    [DataContract]
    public sealed class NewRegexSecretEventArgs : NewSecretEventArgs
    {
        [DataMember]
        public override String Type => "regex";

        public NewRegexSecretEventArgs(String pattern)
        {
            Pattern = pattern;
        }

        [DataMember]
        public String Pattern { get; private set; }
    }

    [DataContract]
    public sealed class NewVariableSecretEventArgs : NewSecretEventArgs
    {
        [DataMember]
        public override String Type => "variable";

        public NewVariableSecretEventArgs(List<string> values)
        {
            Values.AddRange(values);
        }

        [DataMember]
        public List<string> Values { get; private set; } = new List<string>();
    }
}
