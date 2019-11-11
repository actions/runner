using System;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common;
using GitHub.Runner.Listener.Configuration;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class ServiceControlManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Service")]
        public void CalculateServiceName()
        {
            RunnerSettings settings = new RunnerSettings();

            settings.AgentName = "runner";
            settings.ServerUrl = "https://12345678901234567890123456789012345678901234567890.exampledomain.com";
            settings.PoolName = "Default";

            string serviceNamePattern = "actionsrunner.{0}.{1}.{2}";
            string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1}.{2})";

            using (TestHostContext hc = CreateTestContext())
            {
                ServiceControlManager scm = new ServiceControlManager();

                scm.Initialize(hc);
                scm.CalculateServiceName(
                    settings,
                    serviceNamePattern,
                    serviceDisplayNamePattern,
                    out string serviceName,
                    out string serviceDisplayName);

                var serviceNameParts = serviceName.Split('.');

                // Verify name is 79 characters
                Assert.Equal(79, serviceName.Length);

                // Verify nothing has been shortened out
                Assert.Equal("actionsrunner", serviceNameParts[0]);
                Assert.Equal("12345678901234567890123456789012345678901234567890", serviceNameParts[1]);
                Assert.Equal("Default", serviceNameParts[2]);
                Assert.Equal("runner", serviceNameParts[3]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Service")]
        public void CalculateServiceName80Chars()
        {
            RunnerSettings settings = new RunnerSettings();

            settings.AgentName = "runner1";
            settings.ServerUrl = "https://12345678901234567890123456789012345678901234567890.exampledomain.com";
            settings.PoolName = "Default";

            string serviceNamePattern = "actionsrunner.{0}.{1}.{2}";
            string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1}.{2})";

            using (TestHostContext hc = CreateTestContext())
            {
                ServiceControlManager scm = new ServiceControlManager();

                scm.Initialize(hc);
                scm.CalculateServiceName(
                    settings,
                    serviceNamePattern,
                    serviceDisplayNamePattern,
                    out string serviceName,
                    out string serviceDisplayName);

                // Verify name is still equal to 80 characters
                Assert.Equal(80, serviceName.Length);

                var serviceNameParts = serviceName.Split('.');

                // Verify nothing has been shortened out
                Assert.Equal("actionsrunner", serviceNameParts[0]);
                Assert.Equal("12345678901234567890123456789012345678901234567890", serviceNameParts[1]);
                Assert.Equal("Default", serviceNameParts[2]);
                Assert.Equal("runner1", serviceNameParts[3]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Service")]
        public void CalculateServiceNameLimitsServiceNameTo80Chars()
        {
            RunnerSettings settings = new RunnerSettings();

            settings.AgentName = "thisisaverylongrunnernamewithlotsofchars";
            settings.ServerUrl = "https://12345678901234567890123456789012345678901234567890.exampledomain.com";
            settings.PoolName = "thisisaverylongpoolnamewithlotsofchars";

            string serviceNamePattern = "actionsrunner.{0}.{1}.{2}";
            string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1}.{2})";

            using (TestHostContext hc = CreateTestContext())
            {
                ServiceControlManager scm = new ServiceControlManager();

                scm.Initialize(hc);
                scm.CalculateServiceName(
                    settings,
                    serviceNamePattern,
                    serviceDisplayNamePattern,
                    out string serviceName,
                    out string serviceDisplayName);

                // Verify name has been shortened to 80 characters
                Assert.Equal(80, serviceName.Length);

                var serviceNameParts = serviceName.Split('.');

                // Verify that each component has been shortened to a sensible length
                Assert.Equal("actionsrunner", serviceNameParts[0]); // Never shortened
                Assert.Equal("1234567890123456789012345", serviceNameParts[1]); // 25 chars
                Assert.Equal("thisisaverylongpoolnamewi", serviceNameParts[2]); // 25 chars
                Assert.Equal("thisisaverylon", serviceNameParts[3]); // Remainder of unused chars
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            TestHostContext hc = new TestHostContext(this, testName);

            return hc;
        }
    }
}