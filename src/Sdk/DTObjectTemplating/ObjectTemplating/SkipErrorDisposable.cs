using System;

namespace GitHub.DistributedTask.ObjectTemplating
{
    public class SkipErrorDisposable : IDisposable
    {
        public SkipErrorDisposable(TemplateContext context, bool skipError = true)
        {
            m_context = context;
            m_originalSkipError = context.SkipError;
            context.SkipError = skipError;
        }

        public void Dispose()
        {
            m_context.SkipError = m_originalSkipError;
        }

        private TemplateContext m_context;
        private Boolean m_originalSkipError;
    }
}