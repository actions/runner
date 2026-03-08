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
                    string commitHash = ValueOrDefault(options.GlobalOptions, "build_property.CommitHash", "N/A");
                    string packageRuntime = ValueOrDefault(options.GlobalOptions, "build_property.PackageRuntime", "N/A");
                    string runnerVersion = ValueOrDefault(options.GlobalOptions, "build_property.RunnerVersion", "0");
                    return (commitHash, packageRuntime, runnerVersion);
                });

            context.RegisterSourceOutput(props, (ctx, values) =>
            {
                ctx.AddSource("BuildConstants.g.cs", BuildSource(values.CommitHash, values.PackageRuntime, values.RunnerVersion));
            });
        }

        private static string ValueOrDefault(AnalyzerConfigOptions options, string name, string defaultValue)
        {
            options.TryGetValue(name, out string? value);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
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
