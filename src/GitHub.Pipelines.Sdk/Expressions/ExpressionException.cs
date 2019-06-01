using System;
using Microsoft.TeamFoundation.DistributedTask.Logging;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    public class ExpressionException : Exception
    {
        internal ExpressionException(ISecretMasker secretMasker, String message)
        {
            if (secretMasker != null)
            {
                message = secretMasker.MaskSecrets(message);
            }

            m_message = message;
        }

        public override String Message => m_message;

        private readonly String m_message;
    }
}