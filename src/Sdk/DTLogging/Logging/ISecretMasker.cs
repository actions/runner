using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISecretMasker
    {
        int DerivedSecretRecommendedMinimumLength { get; }
        void AddRegex(String pattern);
        void AddValue(String value);
        ISecretMasker Clone();
        String MaskSecrets(String input);
    }
}
