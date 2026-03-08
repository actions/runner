using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace GitHub.Runner.Sdk.Generator
{
    [Generator]
    public sealed class BuildConstantsGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor GitFailedWarning = new DiagnosticDescriptor(
            id: "RUNNER001",
            title: "Git commit hash unavailable",
            messageFormat: "Could not determine git commit hash: {0}. BuildConstants.Source.CommitHash will be empty.",
            category: "BuildConstants",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // PackageRuntime and RunnerVersion come from MSBuild via <CompilerVisibleProperty>.
            // CommitHash is resolved here by running git directly.
            IncrementalValueProvider<(string CommitHash, string? GitError, string PackageRuntime, string RunnerVersion)> props =
                context.AnalyzerConfigOptionsProvider.Select((options, _) =>
                {
                    options.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out string? projectDir);
                    options.GlobalOptions.TryGetValue("build_property.PackageRuntime", out string? packageRuntime);
                    options.GlobalOptions.TryGetValue("build_property.RunnerVersion", out string? runnerVersion);
                    (string commitHash, string? error) = GetGitCommitHash(projectDir ?? string.Empty);
                    return (commitHash, error, packageRuntime ?? string.Empty, runnerVersion ?? string.Empty);
                });

            context.RegisterSourceOutput(props, (ctx, values) =>
            {
                if (values.GitError is not null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(GitFailedWarning, Location.None, values.GitError));
                }
                ctx.AddSource("BuildConstants.g.cs", BuildSource(values.CommitHash, values.PackageRuntime, values.RunnerVersion));
            });
        }

        private static (string Hash, string? Error) GetGitCommitHash(string workingDirectory)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "rev-parse HEAD",
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                string stderr = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    return (output, null);
                }
                return (string.Empty, string.IsNullOrEmpty(stderr) ? $"git exited with code {process.ExitCode}" : stderr);
            }
            catch (Exception ex)
            {
                return (string.Empty, ex.Message);
            }
        }

        private static string BuildSource(string commitHash, string packageRuntime, string runnerVersion)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace GitHub.Runner.Sdk");
            sb.AppendLine("{");
            sb.AppendLine("    public static class BuildConstants");
            sb.AppendLine("    {");
            sb.AppendLine("        public static class Source");
            sb.AppendLine("        {");
            sb.AppendLine($"            public static readonly string CommitHash = \"{commitHash}\";");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static class RunnerPackage");
            sb.AppendLine("        {");
            sb.AppendLine($"            public static readonly string PackageName = \"{packageRuntime}\";");
            sb.AppendLine($"            public static readonly string Version = \"{runnerVersion}\";");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
