using GitHub.DistributedTask.ObjectTemplating;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Runner.Server.Azure.Devops
{
    public class TestGenerator
    {
        public static IEnumerable<TestWorkflow> ResolveWorkflows(string basePath, string folder = "")
        {
            var currentFolder = Path.Combine(basePath, folder);
            var files = Directory.GetFiles(currentFolder, "*.yml");

            bool found = false;

            if (files.Any())
            {
                bool asPipeline = false;

                // prioritize filtering pipeline*.yml files
                var pipelines = files.Where(i => i.Contains($"{Path.DirectorySeparatorChar}pipeline"));
                if (pipelines.Any())
                {
                    asPipeline = true;
                    files = pipelines.ToArray();
                }

                foreach(var file in files)
                {
                    if (TryReadPipeline(basePath, file, asPipeline, out var result))
                    {
                        found = true; // this folder contains pipelines, stop recursion
                        yield return result;
                    }
                }
            }

            if (!found)
            {
                // loop through subfolder
                foreach(var directory in Directory.GetDirectories(currentFolder))
                {
                    var folderName = Path.GetRelativePath(basePath, directory);
                    foreach(var item in ResolveWorkflows(basePath, folderName))
                    {
                        yield return item;
                    }
                }
            }
        }

        private static bool TryReadPipeline(string basePath, string file, bool isPipeline, [MaybeNullWhen(returnValue:false)] out TestWorkflow result)
        {
            result = null;

            // read contents of files
            using(var reader = new StreamReader(file))
            {
                var content = reader.ReadToEnd();

                var parser = new TestWorkflowParser(content);

                if (isPipeline || parser.HasMeta())
                {
                    var relativePath = Path.GetRelativePath(basePath, file);
                    var workingDir = Path.GetDirectoryName(relativePath) ?? basePath;
                    var fileName = Path.GetFileName(relativePath);

                    result = new TestWorkflow(workingDir, fileName)
                    {
                        Name = parser.Name,
                        ValidateSyntax = parser.ValidateSyntax,
                        LocalRepository = parser.LocalRepository,
                        ExpectedException = parser.ExpectedException,
                        ExpectedErrorMessage = parser.ExpectedError
                    };

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Obtains meta-data stored in the yaml file comments
        /// </summary>
        class TestWorkflowParser
        {
            public string? Name { get; private set; }
            public bool ValidateSyntax { get; private set; }
            public Type? ExpectedException { get; private set; }
            public string? ExpectedError { get; private set; }
            public string[] LocalRepository { get; private set; }

            public bool HasMeta()
            {
                return Name != null || ValidateSyntax || ExpectedException != null || ExpectedError != null || LocalRepository?.Length > 0;
            }

            public TestWorkflowParser(string content)
            {
                Name = GetMeta(content, "Name")[0];
                ValidateSyntax = GetMeta(content, "ValidateSyntax")[0] == "true";
                ExpectedError = GetMeta(content, "ExpectedErrorMessage")[0];
                ExpectedException = LoadType(GetMeta(content, "ExpectedException")[0]);
                LocalRepository = GetMeta(content, "LocalRepository").Where(i => i != null).Cast<string>().ToArray();
            }

            private static string?[] GetMeta(string content, string name)
            {
                var matches = Regex.Matches(content, $"^# {name}: (.*?)\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                return matches.Where(i => i.Success).Select(i => i.Groups[1].Value).DefaultIfEmpty().ToArray();
            }

            private static Type? LoadType(string? typeName)
            {
                if (typeName == null)
                {
                    return null;
                }

                var referenceTypes = new Type[]
                {
                    typeof(TemplateValidationError),
                    typeof(Exception)
                };

                // locate and resolve type
                return referenceTypes.Select(i => Type.GetType($"{i.Namespace}.{typeName}, {i.Assembly.GetName().Name}", false)).FirstOrDefault(i => i != null);
            }
        }
    }
}
