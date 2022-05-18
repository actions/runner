using System;
using System.IO;
using System.Linq;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Container
{
    public class FeatureFlagManager
    {
        public static bool IsContainerHooksEnabled(IExecutionContext executionContext)
        {
            var hookExecutablePath = Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath);
            var isContainerHookFeatureFlagSet = executionContext.Global?.Variables.GetBoolean(Constants.Runner.Features.AllowRunnerContainerHooks) ?? false;

            if (isContainerHookFeatureFlagSet && !string.IsNullOrEmpty(hookExecutablePath))
            {
                if (!File.Exists(hookExecutablePath))
                {
                    throw new Exception($"File not found at '{hookExecutablePath}'. Set {Constants.Hooks.ContainerHooksPath} to the path of an existing file.");
                }
                var supportedHookExtensions = new string[] { ".js", ".sh", ".ps1" };
                if (!supportedHookExtensions.Any(extension => hookExecutablePath.EndsWith(extension)))
                {
                    throw new Exception($"Invalid file extension at '{hookExecutablePath}'. {Constants.Hooks.ContainerHooksPath} must be a path to a file with one of the following extensions: {string.Join(", ", supportedHookExtensions)}");
                }
                return true;
            }
            return false;
        }
    }
}
