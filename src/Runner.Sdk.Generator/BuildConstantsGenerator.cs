using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GitHub.Runner.Sdk.Generator
{
    [Generator]
    public sealed class BuildConstantsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // CommitHash, PackageRuntime, and RunnerVersion come from MSBuild via <CompilerVisibleProperty>.
            IncrementalValueProvider<(string CommitHash, string PackageRuntime, string RunnerVersion)> props =
                context.AnalyzerConfigOptionsProvider.Select((options, _) =>
                {
                    string projectDir = ValueOrDefault(options.GlobalOptions, "build_property.MSBuildProjectDirectory", () => string.Empty);
                    string commitHash = ValueOrDefault(options.GlobalOptions, "build_property.CommitHash", () => GetCommitHash(projectDir));
                    string packageRuntime = ValueOrDefault(options.GlobalOptions, "build_property.PackageRuntime", () => GetRuntimeId());
                    string runnerVersion = ValueOrDefault(options.GlobalOptions, "build_property.RunnerVersion", () => GetRunnerVersion(projectDir));
                    return (commitHash, packageRuntime, runnerVersion);
                });

            context.RegisterSourceOutput(props, (ctx, values) =>
            {
                ctx.AddSource("BuildConstants.g.cs", BuildSource(values.CommitHash, values.PackageRuntime, values.RunnerVersion));
            });
        }

        private static string GetCommitHash(string projectDir)
        {
            using var process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
            {
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });
            return process.StandardOutput.ReadToEnd().Trim();
        }

        private static string GetRunnerVersion(string projectDir)
        {
            return File.ReadAllText(Path.Combine(projectDir, "..", "runnerversion")).Trim();
        }

        private static string GetRuntimeId()
        {
            string platform = "unknown";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = "win";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "osx";
            }

            return $"{platform}-{RuntimeInformation.OSArchitecture}".ToLowerInvariant();
        }

        private static string ValueOrDefault(AnalyzerConfigOptions options, string name, Func<string> getDefaultValue)
        {
            options.TryGetValue(name, out string? value);
            if (string.IsNullOrEmpty(value))
            {
                return getDefaultValue();
            }

            return value!;
        }

        private static string BuildSource(string commitHash, string packageRuntime, string runnerVersion)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace GitHub.Runner.Sdk");
            sb.AppendLine("{");
            sb.AppendLine("    /***");
            sb.AppendLine("     * WARNING: This file is automatically regenerated on layout so the runner can provide version/commit info (do not manually edit it).");
            sb.AppendLine("     */");
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
