using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Minimatch;
using System.IO;
using System.Security.Cryptography;
using GitHub.DistributedTask.Expressions2.Sdk;
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

                // Convert slashes on Windows
                if (s_isWindows)
                {
                    pattern = pattern.Replace('\\', '/');
                }

                // Root the pattern
                if (!Path.IsPathRooted(pattern))
                {
                    var patternRoot = s_isWindows ? searchRoot.Replace('\\', '/').TrimEnd('/') : searchRoot.TrimEnd('/');
                    pattern = string.Concat(patternRoot, "/", pattern);
                }

                // Get all files
                context.Trace.Info($"Search root directory: '{searchRoot}'");
                context.Trace.Info($"Search pattern: '{pattern}'");
                var files = Directory.GetFiles(searchRoot, "*", SearchOption.AllDirectories)
                    .Select(x => s_isWindows ? x.Replace('\\', '/') : x)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();
                if (files.Count == 0)
                {
                    throw new ArgumentException($"hashFiles('{ExpressionUtility.StringEscape(pattern)}') failed. Directory '{searchRoot}' is empty");
                }
                else
                {
                    context.Trace.Info($"Found {files.Count} files");
                }

                // Match
                var matcher = new Minimatcher(pattern, s_minimatchOptions);
                files = matcher.Filter(files)
                    .Select(x => s_isWindows ? x.Replace('/', '\\') : x)
                    .ToList();
                if (files.Count == 0)
                {
                    throw new ArgumentException($"hashFiles('{ExpressionUtility.StringEscape(pattern)}') failed. Search pattern '{pattern}' doesn't match any file under '{searchRoot}'");
                }
                else
                {
                    context.Trace.Info($"{files.Count} matches to hash");
                }

                // Hash each file
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

                // Hash the hashes
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

        private static readonly bool s_isWindows = Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;

        // Only support basic globbing (* ? and []) and globstar (**)
        private static readonly Options s_minimatchOptions = new Options
        {
            Dot = true,
            NoBrace = true,
            NoCase = s_isWindows,
            NoComment = true,
            NoExt = true,
            NoNegate = true,
        };
    }
}