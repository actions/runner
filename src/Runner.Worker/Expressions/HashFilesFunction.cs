using System;
using System.IO;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.Runner.Sdk;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;

namespace GitHub.Runner.Worker.Expressions
{
    public sealed class HashFilesFunction : Function
    {
        private const int _hashFileTimeoutSeconds = 120;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var templateContext = context.State as DistributedTask.ObjectTemplating.TemplateContext;
            ArgUtil.NotNull(templateContext, nameof(templateContext));
            templateContext.ExpressionValues.TryGetValue(PipelineTemplateConstants.GitHub, out var githubContextData);
            ArgUtil.NotNull(githubContextData, nameof(githubContextData));
            var githubContext = githubContextData as DictionaryContextData;
            ArgUtil.NotNull(githubContext, nameof(githubContext));
            githubContext.TryGetValue(PipelineTemplateConstants.Workspace, out var workspace);
            var workspaceData = workspace as StringContextData;
            ArgUtil.NotNull(workspaceData, nameof(workspaceData));

            string githubWorkspace = workspaceData.Value;
            bool followSymlink = false;
            List<string> patterns = new List<string>();
            var firstParameter = true;
            foreach (var parameter in Parameters)
            {
                var parameterString = parameter.Evaluate(context).ConvertToString();
                if (firstParameter)
                {
                    firstParameter = false;
                    if (parameterString.StartsWith("--"))
                    {
                        if (string.Equals(parameterString, "--follow-symbolic-links", StringComparison.OrdinalIgnoreCase))
                        {
                            followSymlink = true;
                            continue;
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException($"Invalid glob option {parameterString}, avaliable option: '--follow-symbolic-links'.");
                        }
                    }
                }

                patterns.Add(parameterString);
            }

            context.Trace.Info($"Search root directory: '{githubWorkspace}'");
            context.Trace.Info($"Search pattern: '{string.Join(", ", patterns)}'");

            string binDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string runnerRoot = new DirectoryInfo(binDir).Parent.FullName;

            string node = Path.Combine(runnerRoot, "externals", "node16", "bin", $"node{IOUtil.ExeExtension}");
            string hashFilesScript = Path.Combine(binDir, "hashFiles");
            var hashResult = string.Empty;
            var p = new ProcessInvoker(new HashFilesTrace(context.Trace));
            p.ErrorDataReceived += ((_, data) =>
            {
                if (!string.IsNullOrEmpty(data.Data) && data.Data.StartsWith("__OUTPUT__") && data.Data.EndsWith("__OUTPUT__"))
                {
                    hashResult = data.Data.Substring(10, data.Data.Length - 20);
                    context.Trace.Info($"Hash result: '{hashResult}'");
                }
                else
                {
                    context.Trace.Info(data.Data);
                }
            });

            p.OutputDataReceived += ((_, data) =>
            {
                context.Trace.Info(data.Data);
            });

            var env = new Dictionary<string, string>();
            if (followSymlink)
            {
                env["followSymbolicLinks"] = "true";
            }
            env["patterns"] = string.Join(Environment.NewLine, patterns);

            using (var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_hashFileTimeoutSeconds)))
            {
                try
                {
                    int exitCode = p.ExecuteAsync(workingDirectory: githubWorkspace,
                                                  fileName: node,
                                                  arguments: $"\"{hashFilesScript.Replace("\"", "\\\"")}\"",
                                                  environment: env,
                                                  requireExitCodeZero: false,
                                                  cancellationToken: tokenSource.Token).GetAwaiter().GetResult();

                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException($"hashFiles('{ExpressionUtility.StringEscape(string.Join(", ", patterns))}') failed. Fail to hash files under directory '{githubWorkspace}'");
                    }
                }
                catch (OperationCanceledException) when (tokenSource.IsCancellationRequested)
                {
                    throw new TimeoutException($"hashFiles('{ExpressionUtility.StringEscape(string.Join(", ", patterns))}') couldn't finish within {_hashFileTimeoutSeconds} seconds.");
                }

                return hashResult;
            }
        }

        private sealed class HashFilesTrace : ITraceWriter
        {
            private GitHub.DistributedTask.Expressions2.ITraceWriter _trace;

            public HashFilesTrace(GitHub.DistributedTask.Expressions2.ITraceWriter trace)
            {
                _trace = trace;
            }
            public void Info(string message)
            {
                _trace.Info(message);
            }

            public void Verbose(string message)
            {
                _trace.Info(message);
            }
        }
    }
}