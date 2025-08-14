using System;
using System.IO;
using Xunit;

namespace GitHub.Runner.Common.Tests.Misc
{
    public sealed class SystemdServiceTemplateL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ServiceTemplate_ContainsExpectedKillMode()
        {
            // Arrange
            var templatePath = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "actions.runner.service.template");
            
            // Act
            var templateContent = File.ReadAllText(templatePath);
            
            // Assert
            Assert.Contains("KillMode=mixed", templateContent);
            Assert.Contains("KillSignal=SIGTERM", templateContent);
            Assert.Contains("TimeoutStopSec=5min", templateContent);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ServiceTemplate_HasValidStructure()
        {
            // Arrange
            var templatePath = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "actions.runner.service.template");
            
            // Act
            var templateContent = File.ReadAllText(templatePath);
            var lines = templateContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            // Assert
            Assert.Contains("[Unit]", lines);
            Assert.Contains("[Service]", lines);
            Assert.Contains("[Install]", lines);
            Assert.Contains("Description={{Description}}", lines);
            Assert.Contains("ExecStart={{RunnerRoot}}/runsvc.sh", lines);
            Assert.Contains("User={{User}}", lines);
            Assert.Contains("WorkingDirectory={{RunnerRoot}}", lines);
        }
    }
}