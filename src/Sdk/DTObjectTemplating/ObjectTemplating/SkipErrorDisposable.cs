using System;

namespace GitHub.DistributedTask.ObjectTemplating
{
    public class SkipErrorDisposable : IDisposable
    {
        public SkipErrorDisposable(TemplateContext context, TemplateValidationErrors skipError = null)
        {
            m_context = context;
            m_originalSkipError = context.Errors;
            context.Errors = skipError ?? context.Warnings;
        }

        public void Dispose()
        {
            m_context.Errors = m_originalSkipError;
        }

        private TemplateContext m_context;
        private TemplateValidationErrors m_originalSkipError;
    }
}
