using System;

namespace GitHub.Actions.Expressions
{
    /// <summary>
    /// Used to mask secrets from trace messages and exception messages
    /// </summary>
    public interface ISecretMasker
    {
        String MaskSecrets(String input);
    }
}