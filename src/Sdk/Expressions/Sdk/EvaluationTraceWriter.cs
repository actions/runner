using System;

namespace GitHub.Actions.Expressions.Sdk
{
    internal sealed class EvaluationTraceWriter : ITraceWriter
    {
        public EvaluationTraceWriter(ITraceWriter trace, ISecretMasker secretMasker)
        {
            m_trace = trace;
            m_secretMasker = secretMasker ?? throw new ArgumentNullException(nameof(secretMasker));
        }

        public void Info(String message)
        {
            if (m_trace != null)
            {
                message = m_secretMasker.MaskSecrets(message);
                m_trace.Info(message);
            }
        }

        public void Verbose(String message)
        {
            if (m_trace != null)
            {
                message = m_secretMasker.MaskSecrets(message);
                m_trace.Verbose(message);
            }
        }

        private readonly ISecretMasker m_secretMasker;
        private readonly ITraceWriter m_trace;
    }
}