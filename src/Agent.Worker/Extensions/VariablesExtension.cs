using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Extensions
{
    public static class VariablesExtension
    {
        public static Dictionary<string, VariableValue> ToWebApiVariables(this IEnumerable<Variable> outputVariables)
        {
            var webApiVariables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);

            if (outputVariables == null || !outputVariables.Any())
            {
                return webApiVariables;
            }

            foreach (Variable outputVariable in outputVariables)
            {
                var variableValue = new VariableValue
                {
                    Value = outputVariable.Value,
                    IsSecret = outputVariable.Secret
                };

                webApiVariables.Add(outputVariable.Name, variableValue);
            }

            return webApiVariables;
        }
    }
}