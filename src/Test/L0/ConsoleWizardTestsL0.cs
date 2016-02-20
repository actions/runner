using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Agent.Configuration;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class ConsoleWizardTestsL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConsoleWizard")]
        public void ShouldNotReadFromUser()
        {
            using (TestHostContext thc = new TestHostContext(nameof(ConsoleWizardTestsL0)))
            {
                var consoleWizard = new ConsoleWizard();
                var expectedValue = "ExpectedValue";
                var returnedValue = consoleWizard.GetConfigurationValue(
                    thc,
                    "TestConfigName",
                    new Dictionary<string, ArgumentMetaData> { { "TestConfigName", new ArgumentMetaData() } },
                    new Dictionary<string, string> { { "TestConfigName", expectedValue } },
                    true);
                Assert.True(returnedValue.Equals(expectedValue));
            }
        }
    }
}