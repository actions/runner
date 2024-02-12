#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Worker;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker;

public class SnapshotOperationProviderL0
{
    private TestHostContext? _hc;
    private Mock<IExecutionContext>? _ec;
    private SnapshotOperationProvider? _snapshotOperationProvider;
    private string? _snapshotRequestFilePath;
    private string? _snapshotRequestDirectoryPath; 

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Level", "L0")]
    [Trait("Category", "Worker")]
    public async void CreateSnapshotRequestAsync(bool shouldSnapshotDirectoryAlreadyExist)
    {
        //Arrange
        Setup(shouldSnapshotDirectoryAlreadyExist);
        var expectedSnapshot = new Snapshot(Guid.NewGuid().ToString()); 
        
        //Act
        await _snapshotOperationProvider!.CreateSnapshotRequestAsync(_ec!.Object, expectedSnapshot);
        
        //Assert
        string snapshotRequestJson = await File.ReadAllTextAsync(_snapshotRequestFilePath!);
        var actualSnapshot = JsonConvert.DeserializeObject<Snapshot>(snapshotRequestJson);
        Assert.NotNull(actualSnapshot);
        Assert.Equal(expectedSnapshot.ImageName, actualSnapshot!.ImageName);
        
        _ec.Verify(ec => ec.Write(null, $"A snapshot request was created with parameters: {snapshotRequestJson}"), Times.Once);
        _ec.Verify(ec => ec.Write(null, $"Request written to: {_snapshotRequestFilePath}"), Times.Once);
        _ec.Verify(ec => ec.Write(null, "This request will be processed after the job completes. You will not receive any feedback on the snapshot process within the workflow logs of this job."), Times.Once);
        _ec.Verify(ec => ec.Write(null, "If the snapshot process is successful, you should see a new image with the requested name in the list of available custom images when creating a new GitHub-hosted Runner."), Times.Once);
        _ec.VerifyNoOtherCalls();
    }
    
    private void Setup(bool shouldSnapshotDirectoryAlreadyExist, [CallerMemberName] string testName = "")
    {
        _hc = new TestHostContext(this, testName);
        _ec = new Mock<IExecutionContext>();
        _snapshotOperationProvider = new SnapshotOperationProvider();
        _snapshotOperationProvider.Initialize(_hc);
        _snapshotRequestFilePath = Path.Combine(_hc.GetDirectory(WellKnownDirectory.Root), ".snapshot", "request.json");
        _snapshotRequestDirectoryPath = Path.GetDirectoryName(_snapshotRequestFilePath);

        if (_snapshotRequestDirectoryPath != null)
        {
            // Clean up any existing the snapshot directory and its contents before starting the test. 
            if (Directory.Exists(_snapshotRequestDirectoryPath))
            {
                Directory.Delete(_snapshotRequestDirectoryPath, true);
            }

            if (shouldSnapshotDirectoryAlreadyExist)
            {
                // Create a fresh snapshot directory if it's required for the test. 
                Directory.CreateDirectory(_snapshotRequestDirectoryPath);
            }   
        }
    }
}
