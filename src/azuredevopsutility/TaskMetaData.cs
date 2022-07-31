using Newtonsoft.Json;

public class TaskMetaData {
    public Guid Id { get; set; }
    public string Name { get; set; }

    public TaskVersion Version { get; set; }

    public string ArchivePath { get; set; }

    public static (List<TaskMetaData>, Dictionary<string, TaskMetaData>) LoadTasks(string rootDir) {
        var tasks = new List<TaskMetaData>();
        var tasksByNameAndVersion = new Dictionary<string, TaskMetaData>(StringComparer.OrdinalIgnoreCase);
        foreach(var dir in System.IO.Directory.EnumerateDirectories(rootDir)) {
            var filePath = Path.Join(dir, "task.zip");
            var task = System.IO.Compression.ZipFile.OpenRead(filePath);
            using(var stream = task.GetEntry("task.json")?.Open())
            using(var textreader = new StreamReader(stream)) {
                var metaData = JsonConvert.DeserializeObject<TaskMetaData>(textreader.ReadToEnd());
                metaData.ArchivePath = filePath;
                tasks.Add(metaData);
                tasksByNameAndVersion.Add($"{metaData.Name}@{metaData.Version.Major}", metaData);
                tasksByNameAndVersion.Add($"{metaData.Name}@{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}", metaData);
                tasksByNameAndVersion.Add($"{metaData.Id}@{metaData.Version.Major}", metaData);
                tasksByNameAndVersion.Add($"{metaData.Id}@{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}", metaData);
            }
        }
        return (tasks, tasksByNameAndVersion);
    }
}