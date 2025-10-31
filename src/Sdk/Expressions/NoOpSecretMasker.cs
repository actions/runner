using System;

namespace GitHub.Actions.Expressions
{
    internal sealed class NoOpSecretMasker : ISecretMasker
    {
        public String MaskSecrets(String input)
        {
            return input;
        }
    }
}