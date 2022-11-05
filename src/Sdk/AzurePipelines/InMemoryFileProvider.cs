using System.Collections.Generic;
using System.Linq;
using System;

public class DefaultInMemoryFileProviderFileProvider : IFileProvider
{
    public DefaultInMemoryFileProviderFileProvider(KeyValuePair<string, string>[] workflows, Func<string, string, string> readFile = null) {
        Workflows = workflows.ToDictionary(kv => kv.Key, kv => kv.Value);
        this.readFile = readFile;
    }

    private Func<string, string, string> readFile;
    public Dictionary<string, string> Workflows { get; private set; }

    public string ReadFile(string repositoryAndRef, string path)
    {
        if(repositoryAndRef == null && Workflows.TryGetValue(path, out var content)) {
            return content;
        } else if(readFile != null) {
            return readFile(path, repositoryAndRef);
        }
        return null;
    }
}