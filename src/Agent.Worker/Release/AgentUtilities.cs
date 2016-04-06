using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public static class AgentUtilities
    {
        public static string GetPrintableEnvironmentVariables(IEnumerable<KeyValuePair<string, string>> variables)
        {
            StringBuilder builder = new StringBuilder();

            if (variables != null)
            {
                foreach (var pair in variables)
                {
                    string varName = pair.Key.ToUpperInvariant().Replace(".", "_").Replace(" ", "_");
                    builder.AppendFormat(
                        "\r\n\t\t\t\t[{0}] --> [{1}]",
                        varName,
                        pair.Value);
                }
            }

            return builder.ToString();
        }
    }
}