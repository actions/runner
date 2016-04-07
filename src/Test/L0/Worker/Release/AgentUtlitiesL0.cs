using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.VisualStudio.Services.Agent.Worker.Release;

using Xunit;

namespace Test.L0.Worker.Release
{
    public sealed class AgentUtlitiesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VetGetPrintableEnvironmentVariables()
        {
            List<KeyValuePair<string, string>> variables = new List<KeyValuePair<string, string>>
                                                                      {
                                                                          new KeyValuePair<string, string>("key.1", "value1"),
                                                                          new KeyValuePair<string, string>("key 2", "value2"),
                                                                          new KeyValuePair<string, string>("key3", "value3")
                                                                      };
            string expectedResult =
                $"{Environment.NewLine}\t\t\t\t[{FormatVariable(variables[0].Key)}] --> [{variables[0].Value}]"
                + $"{Environment.NewLine}\t\t\t\t[{FormatVariable(variables[1].Key)}] --> [{variables[1].Value}]"
                + $"{Environment.NewLine}\t\t\t\t[{FormatVariable(variables[2].Key)}] --> [{variables[2].Value}]";

            string result = AgentUtilities.GetPrintableEnvironmentVariables(variables);
            Assert.Equal(expectedResult, result);
        }

        private string FormatVariable(string key)
        {
            return key.ToUpperInvariant().Replace(".", "_").Replace(" ", "_");
        }
    }
}