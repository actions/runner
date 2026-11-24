using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using Moq;
using Moq.Protected;
using Xunit;

namespace GitHub.Runner.Common.Tests;

public sealed class ActionsRunServerL0
{
    [Fact]
    [Trait("Level", "L0")]
    [Trait("Category", "RunnerCommon")]
    public async Task GetJobMessageAsyncCancelsTheHttpRequest()
    {
        using var hostContext = new TestHostContext(this);
        using var cancellation = new CancellationTokenSource();
        var requestToken = CancellationToken.None;
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, token) =>
            {
                requestToken = token;
                cancellation.Cancel();
                return Task.FromCanceled<HttpResponseMessage>(cancellation.Token);
            });

        using var client = new ActionsRunServerHttpClient(new Uri("https://runner.invalid"), handler.Object, disposeHandler: false);
        var locations = new ApiResourceLocationCollection();
        locations.AddResourceLocation(new ApiResourceLocation
        {
            Id = new Guid("25adab70-1379-4186-be8e-b643061ebe3a"),
            Area = "distributedtask",
            ResourceName = "jobmessages",
            RouteTemplate = "_apis/distributedtask/jobmessages/{messageId}",
            MinVersion = new Version(6, 0),
            MaxVersion = new Version(6, 0),
            ReleasedVersion = new Version(6, 0),
            ResourceVersion = 1
        });
        client.SetResourceLocations(locations);

        var server = new ActionsRunServer();
        server.Initialize(hostContext);
        typeof(ActionsRunServer).GetField("_hasConnection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(server, true);
        typeof(ActionsRunServer).GetField("_actionsRunServerClient", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(server, client);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.GetJobMessageAsync("job-id", cancellation.Token));

        Assert.True(requestToken.IsCancellationRequested);
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(request => request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.EndsWith("/job-id", StringComparison.Ordinal)),
            ItExpr.IsAny<CancellationToken>());
    }
}
