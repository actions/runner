using System;
using System.Collections.Generic;
using System.Threading;
using GitHub.Actions.WorkflowParser;
using Newtonsoft.Json;
using Xunit;

namespace GitHub.Runner.Common.Tests.Sdk
{
    public sealed class WorkflowPermissionsL0
    {
        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        [InlineData("code-quality", "none", PermissionLevel.NoAccess)]
        [InlineData("code-quality", "read", PermissionLevel.Read)]
        [InlineData("code-quality", "write", PermissionLevel.Write)]
        [InlineData("copilot-requests", "none", PermissionLevel.NoAccess)]
        [InlineData("copilot-requests", "write", PermissionLevel.Write)]
        public void ParseNewPermissionsAtWorkflowAndJobLevel(string permission, string level, PermissionLevel expected)
        {
            var yaml = $@"name: Permission test
on: push
permissions:
  {permission}: {level}
jobs:
  test:
    runs-on: ubuntu-latest
    permissions:
      {permission}: {level}
    steps:
      - run: echo ok
";

            var workflow = Parse(yaml);

            Assert.Empty(workflow.Errors);
            Assert.NotNull(workflow.Permissions);
            Assert.Equal(expected, GetPermission(workflow.Permissions, permission));
            var job = Assert.Single(workflow.Jobs);
            Assert.NotNull(job.Permissions);
            Assert.Equal(expected, GetPermission(job.Permissions, permission));
            Assert.Equal(expected, GetPermission(workflow.Permissions.Clone(), permission));
            if (expected != PermissionLevel.NoAccess)
            {
                Assert.Contains($"\"{permission}\"", JsonConvert.SerializeObject(workflow.Permissions));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CopilotRequestsRejectsReadLevel()
        {
            var workflow = Parse(@"on: push
permissions:
  copilot-requests: read
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - run: echo ok
");

            Assert.NotEmpty(workflow.Errors);
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        [InlineData(PermissionLevel.Read, PermissionLevel.Read, PermissionLevel.NoAccess)]
        [InlineData(PermissionLevel.Write, PermissionLevel.Write, PermissionLevel.Write)]
        public void ShorthandPermissionsIncludeSupportedScopes(
            PermissionLevel level, PermissionLevel codeQuality, PermissionLevel copilotRequests)
        {
            var permissions = new Permissions(level, includeIdToken: true, includeAttestations: true, includeModels: false);

            Assert.Equal(codeQuality, permissions.CodeQuality);
            Assert.Equal(copilotRequests, permissions.CopilotRequests);
            Assert.Equal(codeQuality, permissions.Clone().CodeQuality);
            Assert.Equal(copilotRequests, permissions.Clone().CopilotRequests);

            var shorthand = level == PermissionLevel.Write ? "write-all" : "read-all";
            var workflow = Parse($@"on: push
permissions: {shorthand}
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - run: echo ok
");

            Assert.Empty(workflow.Errors);
            Assert.Equal(codeQuality, workflow.Permissions.CodeQuality);
            Assert.Equal(copilotRequests, workflow.Permissions.CopilotRequests);
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        [InlineData("code-quality", PermissionLevel.Read)]
        [InlineData("copilot-requests", PermissionLevel.Write)]
        public void PermissionBoundaryIncludesNewScopes(string permission, PermissionLevel level)
        {
            var requested = new Permissions();
            if (permission == "code-quality")
            {
                requested.CodeQuality = level;
            }
            else
            {
                requested.CopilotRequests = level;
            }

            Assert.True(requested.ViolatesMaxPermissions(new Permissions(), out var violations));
            Assert.Single(violations);
        }

        private static WorkflowTemplate Parse(string yaml)
        {
            var parser = new WorkflowTemplateParser(
                null,
                null,
                new ParseOptions { SkipReusableWorkflows = true },
                WorkflowFeatures.GetDefaults());

            return parser.LoadWorkflow(
                new InlineFileProvider(yaml),
                "workflow.yml",
                "Write",
                new Dictionary<string, ReferencedWorkflow>(),
                CancellationToken.None);
        }

        private static PermissionLevel GetPermission(Permissions permissions, string permission)
        {
            return permission == "code-quality" ? permissions.CodeQuality : permissions.CopilotRequests;
        }

        private sealed class InlineFileProvider : IFileProvider
        {
            private readonly string _yaml;

            public InlineFileProvider(string yaml)
            {
                _yaml = yaml;
            }

            public string GetFileContent(string path) => _yaml;
            public string ResolvePath(string defaultRoot, string path) => path;
        }
    }
}
