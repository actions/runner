using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;

using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Release
{
    public sealed class JenkinsArtifactL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IGenericHttpClient> _httpClient;
        private Mock<IExtensionManager> _extensionManager;
        private ArtifactDefinition _artifactDefinition;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void IfNoCommitVersionExistsInArtifactDetailsNoIssueShouldBeAdded()
        {
            using (TestHostContext tc = Setup())
            {
                var trace = tc.GetTrace();

                var artifact = new JenkinsArtifact();
                artifact.Initialize(tc);
                await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, "test");

                _ec.Verify(x => x.AddIssue(It.Is<Issue>(y => y.Type == IssueType.Warning)), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void ShouldLogAnIssueIfEndVersionIsInvalidInArtifactDetail()
        {
            using (TestHostContext tc = Setup())
            {
                var trace = tc.GetTrace();

                JenkinsArtifactDetails details = _artifactDefinition.Details as JenkinsArtifactDetails;
                details.EndCommitArtifactVersion = "xx";
                var artifact = new JenkinsArtifact();
                artifact.Initialize(tc);
                await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, "test");

                _ec.Verify(x => x.AddIssue(It.Is<Issue>(y => y.Type == IssueType.Warning)), Times.Once);
            }
        }

        [Fact]
        [TraitAttribute("Level", "L0")]
        [TraitAttribute("Category", "Worker")]
        public async void MissingStartVersionShouldDownloadCommitsFromSingleBuild()
        {
            using (TestHostContext tc = Setup())
            {
                JenkinsArtifactDetails details = _artifactDefinition.Details as JenkinsArtifactDetails;
                details.EndCommitArtifactVersion = "10";

                var artifact = new JenkinsArtifact();
                artifact.Initialize(tc);
                string expectedUrl = $"{details.Url}/job/{details.JobName}/{details.EndCommitArtifactVersion}/api/json?tree=number,result,changeSet[items[commitId,date,msg,author[fullName]]]";
                await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, tc.GetDirectory(WellKnownDirectory.Root));
                _httpClient.Verify(x => x.GetStringAsync(It.Is<string>(y => y.StartsWith(expectedUrl)), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            }
        }

        [Fact]
        [TraitAttribute("Level", "L0")]
        [TraitAttribute("Category", "Worker")]
        public async void JenkinsCommitsShouldBeFetchedBetweenBuildRange()
        {
            using (TestHostContext tc = Setup())
            {
                JenkinsArtifactDetails details = _artifactDefinition.Details as JenkinsArtifactDetails;
                details.StartCommitArtifactVersion = "10";
                details.EndCommitArtifactVersion = "20";

                var artifact = new JenkinsArtifact();
                artifact.Initialize(tc);

                SetupBuildRangeQuery(details, "{ \"allBuilds\": [{ \"number\": 20 }, { \"number\": 10 }, { \"number\": 2 } ] }");
                string expectedUrl = $"{details.Url}/job/{details.JobName}/api/json?tree=builds[number,result,changeSet[items[commitId,date,msg,author[fullName]]]]{{0,1}}";

                await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, tc.GetDirectory(WellKnownDirectory.Root));
                _httpClient.Verify(x => x.GetStringAsync(It.Is<string>(y => y.StartsWith(expectedUrl)), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            }
        }

        [Fact]
        [TraitAttribute("Level", "L0")]
        [TraitAttribute("Category", "Worker")]
        public async void JenkinsRollbackCommitsNotSupported()
        {
            using (TestHostContext tc = Setup())
            {
                JenkinsArtifactDetails details = _artifactDefinition.Details as JenkinsArtifactDetails;
                details.StartCommitArtifactVersion = "20";
                details.EndCommitArtifactVersion = "10";

                var artifact = new JenkinsArtifact();
                artifact.Initialize(tc);

                SetupBuildRangeQuery(details, "{ \"allBuilds\": [{ \"number\": 20 }, { \"number\": 10 }, { \"number\": 2 } ] }");
                string expectedUrl = $"{details.Url}/job/{details.JobName}/api/json?tree=builds[number,result,changeSet[items[commitId,date,msg,author[fullName]]]]{{0,1}}";

                await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, tc.GetDirectory(WellKnownDirectory.Root));
                _httpClient.Verify(x => x.GetStringAsync(It.Is<string>(y => y.StartsWith(expectedUrl)), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }
        }
        [Fact]
        [TraitAttribute("Level", "L0")]
        [TraitAttribute("Category", "Worker")]
        public async void JenkinsCommitsShouldLogAnIssueIfBuildIsDeleted()
        {
            using (TestHostContext tc = Setup())
            {
                JenkinsArtifactDetails details = _artifactDefinition.Details as JenkinsArtifactDetails;
                details.StartCommitArtifactVersion = "10";
                details.EndCommitArtifactVersion = "20";

                var artifact = new JenkinsArtifact();
                artifact.Initialize(tc);
                SetupBuildRangeQuery(details, "{ \"allBuilds\": [{ \"number\": 30 }, { \"number\": 29 }, { \"number\": 28 } ] }");

                await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, tc.GetDirectory(WellKnownDirectory.Root));

                _ec.Verify(x => x.AddIssue(It.Is<Issue>(y => y.Type == IssueType.Warning)), Times.Once);
            }
        }

        [Fact]
        [TraitAttribute("Level", "L0")]
        [TraitAttribute("Category", "Worker")]
        public async void CommitsShouldBeUploadedAsAttachment()
        {
            using (TestHostContext tc = Setup())
            {
                string commitRootDirectory = tc.GetDirectory(WellKnownDirectory.Root);

                try
                {
                    JenkinsArtifactDetails details = _artifactDefinition.Details as JenkinsArtifactDetails;
                    details.StartCommitArtifactVersion = "10";
                    details.EndCommitArtifactVersion = "20";

                    var artifact = new JenkinsArtifact();
                    artifact.Initialize(tc);

                    SetupBuildRangeQuery(details, "{ \"allBuilds\": [{ \"number\": 20 }, { \"number\": 10 }, { \"number\": 2 } ] }");
                    string commitResult = " {\"builds\": [{ \"number\":9, \"result\":\"SUCCESS\", \"changeSet\": { \"items\": [{ \"commitId\" : \"2869c7ccd0b1b649ba6765e89ee5ff36ef6d4805\", \"author\": { \"fullName\" : \"testuser\" }, \"msg\":\"test\" }]}}]}";
                    string commitsUrl = $"{details.Url}/job/{details.JobName}/api/json?tree=builds[number,result,changeSet[items[commitId,date,msg,author[fullName]]]]{{0,1}}";
                    _httpClient.Setup(x => x.GetStringAsync(It.Is<string>(y => y.StartsWith(commitsUrl)), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                                .Returns(Task.FromResult(commitResult));

                    string commitFilePath = Path.Combine(commitRootDirectory, $"commits_{details.Alias}_1.json");
                    Directory.CreateDirectory(commitRootDirectory);

                    await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, commitRootDirectory);
                    _ec.Verify(x => x.QueueAttachFile(It.Is<string>(y => y.Equals(CoreAttachmentType.FileAttachment)), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
                }
                finally
                {
                    IOUtil.DeleteDirectory(commitRootDirectory, CancellationToken.None);
                }
            }
        }

        [Fact]
        [TraitAttribute("Level", "L0")]
        [TraitAttribute("Category", "Worker")]
        public async void CommitsShoulHaveUrlIfItsGitRepo()
        {
            using (TestHostContext tc = Setup())
            {
                string commitRootDirectory = tc.GetDirectory(WellKnownDirectory.Root);

                try
                {
                    JenkinsArtifactDetails details = _artifactDefinition.Details as JenkinsArtifactDetails;
                    details.StartCommitArtifactVersion = "10";
                    details.EndCommitArtifactVersion = "20";

                    var artifact = new JenkinsArtifact();
                    artifact.Initialize(tc);

                    SetupBuildRangeQuery(details, "{ \"allBuilds\": [{ \"number\": 20 }, { \"number\": 10 }, { \"number\": 2 } ] }");
                    string commitResult = " {\"builds\": [{ \"number\":9, \"result\":\"SUCCESS\", \"changeSet\": { \"items\": [{ \"commitId\" : \"2869c7ccd0b1b649ba6765e89ee5ff36ef6d4805\", \"author\": { \"fullName\" : \"testuser\" }, \"msg\":\"test\" }]}}]}";
                    string commitsUrl = $"{details.Url}/job/{details.JobName}/api/json?tree=builds[number,result,changeSet[items[commitId,date,msg,author[fullName]]]]{{0,1}}";
                    _httpClient.Setup(x => x.GetStringAsync(It.Is<string>(y => y.StartsWith(commitsUrl)), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                                .Returns(Task.FromResult(commitResult));

                    string repoUrl = $"{details.Url}/job/{details.JobName}/{details.EndCommitArtifactVersion}/api/json?tree=actions[remoteUrls],changeSet[kind]";
                    string repoResult = "{ \"actions\": [ { \"remoteUrls\": [ \"https://github.com/TestUser/TestRepo\" ] }, ], \"changeSet\": { \"kind\": \"git\" } }";
                    _httpClient.Setup(x => x.GetStringAsync(It.Is<string>(y => y.StartsWith(repoUrl)), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                                .Returns(Task.FromResult(repoResult));

                    string commitFilePath = Path.Combine(commitRootDirectory, $"commits_{details.Alias}_1.json");
                    Directory.CreateDirectory(commitRootDirectory);

                    string expectedCommitUrl = "https://github.com/TestUser/TestRepo/commit/2869c7ccd0b1b649ba6765e89ee5ff36ef6d4805";
                    await artifact.DownloadCommitsAsync(_ec.Object, _artifactDefinition, commitRootDirectory);
                    _ec.Verify(x => x.QueueAttachFile(It.Is<string>(y => y.Equals(CoreAttachmentType.FileAttachment)), It.IsAny<string>(), It.Is<string>(z => string.Join("", File.ReadAllLines(z)).Contains(expectedCommitUrl))), Times.Once);
                }
                finally
                {
                    IOUtil.DeleteDirectory(commitRootDirectory, CancellationToken.None);
                }
            }
        }

        private void SetupBuildRangeQuery(JenkinsArtifactDetails details, string result)
        {
            string buildIndexUrl = $"{details.Url}/job/{details.JobName}/api/json?tree=allBuilds[number]";
            _httpClient.Setup(x => x.GetStringAsync(It.Is<string>(y => y.StartsWith(buildIndexUrl)), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                        .Returns(Task.FromResult(result));
        }

        private TestHostContext Setup([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
            _httpClient = new Mock<IGenericHttpClient>();
            _artifactDefinition = new ArtifactDefinition
            {
                Details = new JenkinsArtifactDetails
                {
                    Url = new Uri("http://localhost"),
                    JobName = "jenkins",
                    Alias = "jenkins"
                }
            };

            _extensionManager = new Mock<IExtensionManager>();

            hc.SetSingleton<IExtensionManager>(_extensionManager.Object);
            hc.SetSingleton<IGenericHttpClient>(_httpClient.Object);

            return hc;
        }
    }
}