using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    // TODO: Consider adding a cancellation token to IContext.
    public interface IHostContext : IContext
    {
    }

    public sealed class HostContext : Context, IHostContext
    {
        protected override void Write(LogLevel level, String message)
        {
            throw new System.NotImplementedException();
        }
    }
}