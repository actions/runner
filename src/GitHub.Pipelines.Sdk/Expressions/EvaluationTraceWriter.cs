using System;
using Microsoft.TeamFoundation.DistributedTask.Logging;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal sealed class EvaluationTraceWriter : ITraceWriter
    {
        public EvaluationTraceWriter(ITraceWriter trace, ISecretMasker secretMasker)
        {
            ArgumentUtility.CheckForNull(secretMasker, nameof(secretMasker));
            m_trace = trace;
            m_secretMasker = secretMasker;
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