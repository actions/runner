using System;
using System.ComponentModel;

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
    }
}
