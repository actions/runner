using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal static class JobEnvironmentExtensions
    {
        public static Dictionary<String, String> GetProcessParameters(this JobEnvironment environment)
        {
            // find the environment variables which satisfy the process parameter regex
            var processVariablesDictionary = environment.Variables
                .Where(v => s_processParameterRegex.Value.IsMatch(v.Key))
                .ToDictionary(v => v.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
            return processVariablesDictionary;
        }

        // Case insensitive regex to match the process parameter like Parameters.param or ProcParam.param
        private static readonly Lazy<Regex> s_processParameterRegex = new Lazy<Regex>(() => new Regex(@"^(Parameters|ProcParam)[\.]([^)]+)$", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase), true);
    }
}
