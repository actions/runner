using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseDirectoryManager : AgentService, IReleaseDirectoryManager
    {
        public ReleaseDefinitionToFolderMap PrepareArtifactsDirectory(
            string workingDirectory,
            string collectionId,
            string projectId,
            string releaseDefinitionId)
        {
            Trace.Entering();
            ReleaseDefinitionToFolderMap map = null;
            string mapFile = Path.Combine(
                workingDirectory,
                Constants.Release.Path.RootMappingDirectory,
                collectionId,
                projectId,
                releaseDefinitionId,
                Constants.Release.Path.DefinitionMapping);

            Trace.Verbose($"Mappings file: {mapFile}");
            map = LoadIfExists(mapFile);
            if (map == null)
            {
                Trace.Verbose("Mappings file does not exist. A new mapping file will be created");
                var releaseDirectorySuffix = ComputeFolderInteger(workingDirectory);
                map = new ReleaseDefinitionToFolderMap();
                map.ReleaseDirectory = string.Format(
                    "{0}{1}",
                    Constants.Release.Path.ReleaseDirectoryPrefix,
                    releaseDirectorySuffix);
                WriteToFile(mapFile, map);
                Trace.Verbose($"Created a new mapping file: {mapFile}");
            }

            return map;
        }

        private int ComputeFolderInteger(string workingDirectory)
        {
            Trace.Entering();
            Regex regex = new Regex(string.Format(@"^{0}[0-9]*$", Constants.Release.Path.ReleaseDirectoryPrefix));
            var dirs = Directory.GetDirectories(workingDirectory);
            var folderNames = dirs.Select(Path.GetFileName).Where(name => regex.IsMatch(name));
            Trace.Verbose($"Number of folder with integer names: {folderNames.Count()}");

            if (folderNames.Any())
            {
                var max = folderNames.Select(x => Int32.Parse(x.Substring(1))).Max();
                return max + 1;
            }
            else
            {
                return 1;
            }
        }

        private ReleaseDefinitionToFolderMap LoadIfExists(string mappingFile)
        {
            Trace.Entering();
            Trace.Verbose($"Loading mapping file: {mappingFile}");
            if (!File.Exists(mappingFile))
            {
                return null;
            }

            string content = File.ReadAllText(mappingFile);
            var map = JsonConvert.DeserializeObject<ReleaseDefinitionToFolderMap>(content);
            return map;
        }

        private void WriteToFile(string file, object value)
        {
            Trace.Entering();
            Trace.Verbose($"Writing config to file: {file}");

            // Create the directory if it does not exist.
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            IOUtil.SaveObject(value, file);
        }
    }
}