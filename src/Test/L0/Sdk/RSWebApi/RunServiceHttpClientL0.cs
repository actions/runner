using GitHub.Actions.RunService.WebApi;
using Xunit;

namespace GitHub.Actions.RunService.WebApi.Tests;

public sealed class RunServiceHttpClientL0
{
    [Fact]
    public void Truncate()
    {
        TestTruncate(string.Empty.PadLeft(199, 'a'), string.Empty.PadLeft(199, 'a'));
        TestTruncate(string.Empty.PadLeft(200, 'a'), string.Empty.PadLeft(200, 'a'));
        TestTruncate(string.Empty.PadLeft(201, 'a'), string.Empty.PadLeft(200, 'a') + "[truncated]");
    }

    private void TestTruncate(string errorBody, string expected)
    {
        Assert.Equal(expected, RunServiceHttpClient.Truncate(errorBody));
    }
}
