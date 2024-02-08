using System.IO;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using Newtonsoft.Json;

namespace GitHub.Runner.Worker;

[ServiceLocator(Default = typeof(SnapshotOperationProvider))]
public interface ISnapshotOperationProvider : IRunnerService
{
    Task CreateSnapshotRequestAsync(IExecutionContext executionContext, Snapshot snapshotRequest);
}

public class SnapshotOperationProvider : RunnerService, ISnapshotOperationProvider
{
    public async Task CreateSnapshotRequestAsync(IExecutionContext executionContext, Snapshot snapshotRequest)
    {
        var snapshotRequestFilePath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Root), ".snapshot", "request.json");
        string snapshotRequestDirectoryPath = Path.GetDirectoryName(snapshotRequestFilePath);
        if (snapshotRequestDirectoryPath != null)
        {
            Directory.CreateDirectory(snapshotRequestDirectoryPath);
        }

        var snapshotRequestJson = JsonConvert.SerializeObject(snapshotRequest);
        await File.WriteAllTextAsync(snapshotRequestFilePath, snapshotRequestJson);
        executionContext.Output($"A snapshot request was created with parameters: {snapshotRequestJson}");
        executionContext.Output($"Request written to: {snapshotRequestFilePath}");
        executionContext.Output("This request will be processed after the job completes. You will not receive any feedback on the snapshot process within the workflow logs of this job.");
        executionContext.Output("If the snapshot process is successful, you should see a new image with the requested name in the list of available custom images when creating a new GitHub-hosted Runner.");
        await Task.CompletedTask;
    }
}
