using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class TaskMetaData {
    public Guid Id { get; set; }
    public string Name { get; set; }

    public TaskVersion Version { get; set; }

    public string ArchivePath { get; set; }
    public Func<ControllerContext, IActionResult> ArchiveCallback { get; set; }

    public static bool ValidZipFile(string filePath) {
        try {
            return System.IO.Compression.ZipFile.OpenRead(filePath).Entries.Any();
        } catch {
        }
        return false;
    }

    public static (List<TaskMetaData>, Dictionary<string, TaskMetaData>) LoadTasks(string rootDir) {
        var tasks = new List<TaskMetaData>();
        var tasksByNameAndVersion = new Dictionary<string, TaskMetaData>(StringComparer.OrdinalIgnoreCase);
        try {
            foreach(var dir in System.IO.Directory.EnumerateDirectories(rootDir)) {
                try {
                    var filePath = Path.Join(dir, "task.zip");
                    var task = System.IO.Compression.ZipFile.OpenRead(filePath);
                    using(var stream = task.GetEntry("task.json")?.Open())
                    using(var textreader = new StreamReader(stream)) {
                        var metaData = JsonConvert.DeserializeObject<TaskMetaData>(textreader.ReadToEnd());
                        metaData.ArchivePath = filePath;
                        tasks.Add(metaData);
                        if(!tasksByNameAndVersion.TryGetValue($"{metaData.Id}@{metaData.Version.Major}", out var ometaData) || ometaData.Version.Minor <= metaData.Version.Minor) {
                            tasksByNameAndVersion[$"{metaData.Name}@{metaData.Version.Major}"] = metaData;
                            tasksByNameAndVersion[$"{metaData.Id}@{metaData.Version.Major}"] = metaData;
                        }
                        tasksByNameAndVersion[$"{metaData.Name}@{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}"] = metaData;
                        tasksByNameAndVersion[$"{metaData.Id}@{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}"] = metaData;
                    }
                } catch {

                }
            }
        } catch {

        }
        return (tasks, tasksByNameAndVersion);
    }
}