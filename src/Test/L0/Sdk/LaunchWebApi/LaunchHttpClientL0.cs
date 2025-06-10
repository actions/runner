using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Launch.Client;
using GitHub.Services.Launch.Contracts;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Actions.RunService.WebApi.Tests
{
    public sealed class LaunchHttpClientL0
    {
        [Fact]
        public async Task GetResolveActionsDownloadInfoAsync_SuccessResponse()
        {
            var baseUrl = new Uri("https://api.github.com/");
            var planId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var token = "fake-token";

            var actionReferenceList = new ActionReferenceList
            {
                Actions = new List<ActionReference>
                {
                    new ActionReference
                    {
                        NameWithOwner = "owner1/action1",
                        Ref = "0123456789"
                    }
                }
            };

            var responseContent = @"{
                ""actions"": {
                    ""owner1/action1@0123456789"": {
                        ""name"": ""owner1/action1"",
                        ""resolved_name"": ""owner1/action1"",
                        ""resolved_sha"": ""0123456789"",
                        ""version"": ""0123456789"",
                        ""zip_url"": ""https://github.com/owner1/action1/zip"",
                        ""tar_url"": ""https://github.com/owner1/action1/tar""
                    }
                }
            }";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json"),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri($"{baseUrl}actions/build/{planId}/jobs/{jobId}/runnerresolve/actions")
                }
            };

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var client = new LaunchHttpClient(baseUrl, mockHandler.Object, token, false);
            var result = await client.GetResolveActionsDownloadInfoAsyncV2(planId, jobId, actionReferenceList, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Actions);
            Assert.Equal(actionReferenceList.Actions.Count, result.Actions.Count);
            Assert.True(result.Actions.ContainsKey("owner1/action1@0123456789"));
        }

        [Fact]
        public async Task GetResolveActionsDownloadInfoAsync_UnprocessableEntityResponse()
        {
            var baseUrl = new Uri("https://api.github.com/");
            var planId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var token = "fake-token";

            var actionReferenceList = new ActionReferenceList
            {
                Actions = new List<ActionReference>
                {
                    new ActionReference
                    {
                        NameWithOwner = "owner1/action1",
                        Ref = "0123456789"
                    }
                }
            };

            var responseContent = @"{
                ""errors"": {
                    ""owner1/invalid-action@0123456789"": {
                        ""message"": ""Unable to resolve action 'owner1/invalid-action@0123456789', repository not found""
                    }
                }
            }";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json"),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri($"{baseUrl}actions/build/{planId}/jobs/{jobId}/runnerresolve/actions")
                }
            };

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var client = new LaunchHttpClient(baseUrl, mockHandler.Object, token, false);

            var exception = await Assert.ThrowsAsync<UnresolvableActionDownloadInfoException>(
                () => client.GetResolveActionsDownloadInfoAsyncV2(planId, jobId, actionReferenceList, CancellationToken.None));
            
            Assert.Contains("repository not found", exception.Message);
        }
    }
}