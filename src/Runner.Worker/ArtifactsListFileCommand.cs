using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Container;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker
{
    /// <summary>
    /// File command extension that exposes the job-scoped aggregate of
    /// <see cref="GlobalContext.ArtifactSubjects"/> as a read-only JSON
    /// file. Subsequent steps in the same job read the file via the
    /// <c>GITHUB_ARTIFACTS_LIST</c> environment variable, getting a
    /// running view of every artifact declared via
    /// <c>$GITHUB_ARTIFACTS</c> in earlier steps.
    /// </summary>
    /// <remarks>
    /// The file uses the existing per-step file-command lifecycle:
    /// <see cref="FileCommandManager.InitializeFiles"/> creates a fresh
    /// file, invokes <see cref="PopulateInitialContents"/> here, exposes
    /// the (translated) path to the step's environment, and (for the
    /// read-only file) ignores anything the step writes back.
    ///
    /// The file is always written when the feature is enabled, so
    /// consumers never need to branch on "did the runner inject this?".
    /// An empty aggregate produces <c>{"version":1,"subjects":[]}</c>.
    /// </remarks>
    public sealed class ArtifactsListFileCommand : RunnerService, IFileCommandExtension
    {
        public const int FormatVersion = 1;

        public string ContextName => "artifacts_list";
        public string FilePrefix => "artifacts_list_";

        public Type ExtensionType => typeof(IFileCommandExtension);

        public void PopulateInitialContents(IExecutionContext context, string filePath, ContainerInfo container)
        {
            ArgUtil.NotNull(context, nameof(context));

            // Feature flag gate. Mirrors CreateArtifactsFileCommand so the
            // write side and the read side are toggled together.
            var enabled = (context.Global.Variables.GetBoolean(Constants.Runner.Features.AllowArtifactsFile) ?? false)
                || StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable(CreateArtifactsFileCommand.EnableEnvVar));
            if (!enabled)
            {
                Trace.Verbose("$GITHUB_ARTIFACTS_LIST publishing is disabled (feature flag and env-var fallback are both off).");
                return;
            }

            var aggregate = context.Global.ArtifactSubjects
                ?? new Dictionary<string, ArtifactSubject>(StringComparer.Ordinal);

            var subjects = new JArray();
            // Emit subjects sorted by name so the output is deterministic
            // regardless of the backing dictionary's enumeration order
            // (which is not contractually guaranteed).
            foreach (var entry in aggregate.Values.OrderBy(v => v.Name, StringComparer.Ordinal))
            {
                subjects.Add(new JObject
                {
                    ["name"] = entry.Name,
                    ["digest"] = entry.Digest,
                    ["kind"] = entry.Kind == ArtifactSubjectKind.OciSubject ? "oci" : "file",
                });
            }

            var payload = new JObject
            {
                ["version"] = FormatVersion,
                ["subjects"] = subjects,
            };

            // UTF-8 without BOM; consumers in other languages should not
            // have to special-case a leading BOM.
            File.WriteAllText(filePath, payload.ToString(Formatting.None), new UTF8Encoding(false));
            Trace.Info($"Wrote $GITHUB_ARTIFACTS_LIST with {aggregate.Count} subject(s) to '{filePath}'");
        }

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            // Read-only file: anything the step writes here is ignored.
            // The aggregate is fed only by the write-side $GITHUB_ARTIFACTS
            // file processed by CreateArtifactsFileCommand.
        }
    }
}
