using System;

namespace GitHub.Runner.Common
{
    public interface IExtension : IRunnerService
    {
        Type ExtensionType { get; }
    }
}
