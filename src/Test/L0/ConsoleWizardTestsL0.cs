using Microsoft.VisualStudio.Services.Agent.Configuration;
using System;
using System.Collections.Generic;
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
                consoleWizard.Initialize(thc);
                var expectedValue = "ExpectedValue";
                var returnedValue = consoleWizard.ReadValue(
                    "TestConfigName",
                    "Test Config Name",
                    false,
                    String.Empty,
                    Validators.NonEmptyValidator,
                    new Dictionary<string, string> { { "TestConfigName", expectedValue } },
                    unattended: true);

                Assert.True(returnedValue.Equals(expectedValue));
            }
        }
    }
}