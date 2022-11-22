using System.Runtime.CompilerServices;
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
            RunnerSettings settings = new();

            settings.AgentName = "thisiskindofalongrunnerabcde";
            settings.ServerUrl = "https://example.githubusercontent.com/12345678901234567890123456789012345678901234567890";
            settings.GitHubUrl = "https://github.com/myorganizationexample/myrepoexample";

            string serviceNamePattern = "actions.runner.{0}.{1}";
            string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1})";

            using (TestHostContext hc = CreateTestContext())
            {
                ServiceControlManager scm = new();

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
                Assert.Equal("actions", serviceNameParts[0]);
                Assert.Equal("runner", serviceNameParts[1]);
                Assert.Equal("myorganizationexample-myrepoexample", serviceNameParts[2]); // '/' has been replaced with '-'
                Assert.Equal("thisiskindofalongrunnerabcde", serviceNameParts[3]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Service")]
        public void CalculateServiceName80Chars()
        {
            RunnerSettings settings = new();

            settings.AgentName = "thisiskindofalongrunnernabcde";
            settings.ServerUrl = "https://example.githubusercontent.com/12345678901234567890123456789012345678901234567890";
            settings.GitHubUrl = "https://github.com/myorganizationexample/myrepoexample";

            string serviceNamePattern = "actions.runner.{0}.{1}";
            string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1})";

            using (TestHostContext hc = CreateTestContext())
            {
                ServiceControlManager scm = new();

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
                Assert.Equal("actions", serviceNameParts[0]);
                Assert.Equal("runner", serviceNameParts[1]);
                Assert.Equal("myorganizationexample-myrepoexample", serviceNameParts[2]); // '/' has been replaced with '-'
                Assert.Equal("thisiskindofalongrunnernabcde", serviceNameParts[3]); // should not have random numbers unless we exceed 80
            }
        }

        #if OS_WINDOWS
            [Fact]
            [Trait("Level", "L0")]
            [Trait("Category", "Service")]
            public void CalculateServiceNameLimitsServiceNameTo80Chars()
            {
                RunnerSettings settings = new RunnerSettings();

                settings.AgentName = "thisisareallyreallylongbutstillvalidagentname";
                settings.ServerUrl = "https://example.githubusercontent.com/12345678901234567890123456789012345678901234567890";
                settings.GitHubUrl = "https://github.com/myreallylongorganizationexample/myreallylongrepoexample";

                string serviceNamePattern = "actions.runner.{0}.{1}";
                string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1})";

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
                    Assert.Equal("actions", serviceNameParts[0]); // Never shortened
                    Assert.Equal("runner", serviceNameParts[1]); // Never shortened
                    Assert.Equal("myreallylongorganizationexample-myreallylongr", serviceNameParts[2]); // First 45 chars, '/' has been replaced with '-'
                    Assert.Matches(@"^(thisisareallyr-[0-9]{4})$", serviceNameParts[3]); // Remainder of unused chars, 4 random numbers added at the end
                }
            }

            // Special 'defensive' test that verifies we can gracefully handle creating service names
            // in case GitHub.com changes its org/repo naming convention in the future,
            // and some of these characters may be invalid for service names
            // Not meant to test character set exhaustively -- it's just here to exercise the sanitizing logic
            [Fact]
            [Trait("Level", "L0")]
            [Trait("Category", "Service")]
            public void CalculateServiceNameSanitizeOutOfRangeChars()
            {
                RunnerSettings settings = new RunnerSettings();

                settings.AgentName = "name";
                settings.ServerUrl = "https://example.githubusercontent.com/12345678901234567890123456789012345678901234567890";
                settings.GitHubUrl = "https://github.com/org!@$*+[]()/repo!@$*+[]()";

                string serviceNamePattern = "actions.runner.{0}.{1}";
                string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1})";

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

                    // Verify service name parts are sanitized correctly
                    Assert.Equal("actions", serviceNameParts[0]);
                    Assert.Equal("runner", serviceNameParts[1]);
                    Assert.Equal("org----------repo---------", serviceNameParts[2]); // Chars replaced with '-'
                    Assert.Equal("name", serviceNameParts[3]);
                }
            }
        #else
            [Fact]
            [Trait("Level", "L0")]
            [Trait("Category", "Service")]
            public void CalculateServiceNameLimitsServiceNameTo150Chars()
            {
                RunnerSettings settings = new();

                settings.AgentName = "thisisareallyreallylongbutstillvalidagentnameiamusingforthisexampletotestverylongnamelimits";
                settings.ServerUrl = "https://example.githubusercontent.com/12345678901234567890123456789012345678901234567890";
                settings.GitHubUrl = "https://github.com/myreallylongorganizationexampleonlinux/myreallylongrepoexampleonlinux1234";

                string serviceNamePattern = "actions.runner.{0}.{1}";
                string serviceDisplayNamePattern = "GitHub Actions Runner ({0}.{1})";

                using (TestHostContext hc = CreateTestContext())
                {
                    ServiceControlManager scm = new();

                    scm.Initialize(hc);
                    scm.CalculateServiceName(
                        settings,
                        serviceNamePattern,
                        serviceDisplayNamePattern,
                        out string serviceName,
                        out string serviceDisplayName);

                    // Verify name has been shortened to 150
                    Assert.Equal(150, serviceName.Length);

                    var serviceNameParts = serviceName.Split('.');

                    // Verify that each component has been shortened to a sensible length
                    Assert.Equal("actions", serviceNameParts[0]); // Never shortened
                    Assert.Equal("runner", serviceNameParts[1]); // Never shortened
                    Assert.Equal("myreallylongorganizationexampleonlinux-myreallylongrepoexampleonlinux1", serviceNameParts[2]); // First 70 chars, '/' has been replaced with '-'
                    Assert.Matches(@"^(thisisareallyreallylongbutstillvalidagentnameiamusingforthi-[0-9]{4})$", serviceNameParts[3]);
                }
            }
        #endif

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            TestHostContext hc = new(this, testName);

            return hc;
        }
    }
}