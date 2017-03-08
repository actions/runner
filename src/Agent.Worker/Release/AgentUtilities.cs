using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public static class AgentUtilities
    {
        // Move this to Agent.Common.Util
        public static string GetPrintableEnvironmentVariables(IEnumerable<KeyValuePair<string, string>> variables)
        {
            StringBuilder builder = new StringBuilder();

            if (variables != null)
            {
                var sortedVariables = variables.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
                foreach (var pair in sortedVariables)
                {
                    string varName = pair.Key.ToUpperInvariant().Replace(".", "_").Replace(" ", "_");
                    builder.AppendFormat(
                        "{0}\t\t\t\t[{1}] --> [{2}]",
                        Environment.NewLine,
                        varName,
                        pair.Value);
                }
            }

            return builder.ToString();
        }
    }
}