using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Runner.Server.Azure.Devops {

    public class DefaultInMemoryFileProviderFileProvider : IFileProvider
    {
        public DefaultInMemoryFileProviderFileProvider(KeyValuePair<string, string>[] workflows, Func<string, string, string> readFile = null) {
            Workflows = workflows.ToDictionary(kv => kv.Key, kv => kv.Value);
            this.readFile = readFile;
        }

        private Func<string, string, string> readFile;
        public Dictionary<string, string> Workflows { get; private set; }

        public Task<string> ReadFile(string repositoryAndRef, string path)
        {
            if(repositoryAndRef == null && Workflows.TryGetValue(path, out var content)) {
                return Task.FromResult(content);
            } else if(readFile != null) {
                return Task.FromResult(readFile(path, repositoryAndRef));
            }
            return Task.FromResult<string>(null);
        }
    }
}