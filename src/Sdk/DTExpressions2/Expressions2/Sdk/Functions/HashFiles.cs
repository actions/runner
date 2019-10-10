using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Minimatch;
using System.IO;
using System.Security.Cryptography;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class HashFiles : Function
    {
        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;

            // hashFiles() only works on the runner and only works with files under GITHUB_WORKSPACE
            // Since GITHUB_WORKSPACE is set by runner, I am using that as the fact of this code runs on server or runner.
            if (context.State is ObjectTemplating.TemplateContext templateContext &&
                templateContext.ExpressionValues.TryGetValue(PipelineTemplateConstants.GitHub, out var githubContextData) &&
                githubContextData is DictionaryContextData githubContext &&
                githubContext.TryGetValue(PipelineTemplateConstants.Workspace, out var workspace) == true &&
                workspace is StringContextData workspaceData)
            {
                string searchRoot = workspaceData.Value;
                string pattern = Parameters[0].Evaluate(context).ConvertToString();

                context.Trace.Info($"Search root directory: '{searchRoot}'");
                context.Trace.Info($"Search pattern: '{pattern}'");
                var files = Directory.GetFiles(searchRoot, "*", SearchOption.AllDirectories).OrderBy(x => x).ToList();
                if (files.Count == 0)
                {
                    throw new ArgumentException($"'hashFiles({pattern})' failed. Directory '{searchRoot}' is empty");
                }
                else
                {
                    context.Trace.Info($"Found {files.Count} files");
                }

                var matcher = new Minimatcher(pattern, s_minimatchOptions);
                files = matcher.Filter(files).ToList();
                if (files.Count == 0)
                {
                    throw new ArgumentException($"'hashFiles({pattern})' failed. Search pattern '{pattern}' doesn't match any file under '{searchRoot}'");
                }
                else
                {
                    context.Trace.Info($"{files.Count} matches to hash");
                }

                List<byte> filesSha256 = new List<byte>();
                foreach (var file in files)
                {
                    context.Trace.Info($"Hash {file}");
                    using (SHA256 sha256hash = SHA256.Create())
                    {
                        using (var fileStream = File.OpenRead(file))
                        {
                            filesSha256.AddRange(sha256hash.ComputeHash(fileStream));
                        }
                    }
                }

                using (SHA256 sha256hash = SHA256.Create())
                {
                    var hashBytes = sha256hash.ComputeHash(filesSha256.ToArray());
                    StringBuilder hashString = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        hashString.Append(hashBytes[i].ToString("x2"));
                    }
                    var result = hashString.ToString();
                    context.Trace.Info($"Final hash result: '{result}'");
                    return result;
                }
            }
            else
            {
                throw new InvalidOperationException("'hashfiles' expression function is only supported under runner context.");
            }
        }

        private static readonly Options s_minimatchOptions = new Options
        {
            Dot = true,
            NoBrace = true,
            NoCase = Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX
        };
    }
}