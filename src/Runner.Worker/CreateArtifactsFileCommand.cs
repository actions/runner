using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Container;

namespace GitHub.Runner.Worker
{
    /// <summary>
    /// File command extension that implements the <c>GITHUB_ARTIFACTS</c>
    /// per-step environment file contract.
    /// </summary>
    /// <remarks>
    /// Lifecycle is identical to the other per-step file commands:
    /// <see cref="FileCommandManager"/> creates an empty file before each
    /// step runs and invokes <see cref="ProcessCommand"/> after the step
    /// completes. This class is responsible for parsing the file's
    /// contents, validating each entry, and aggregating the resulting
    /// (name, digest) pairs onto <see cref="GlobalContext.ArtifactSubjects"/>
    /// at job scope.
    ///
    /// The feature is gated by the <c>actions_runner_allow_artifacts_file</c>
    /// feature flag. When the flag is disabled, the env var is still
    /// exposed but writes are silently ignored.
    /// </remarks>
    public sealed class CreateArtifactsFileCommand : RunnerService, IFileCommandExtension
    {
        // Each per-step file may contain at most 1 MiB.
        public const int MaxFileSizeBytes = 1024 * 1024;

        // A job may declare at most 500 artifacts in aggregate.
        public const int MaxAggregateArtifacts = 500;

        public string ContextName => "artifacts";
        public string FilePrefix => "artifacts_";

        // Runner-side environment variable that enables the feature on
        // self-hosted runners where the server-side feature flag is not
        // configurable. Mirrors patterns like
        // ACTIONS_RUNNER_COMPARE_WORKFLOW_PARSER elsewhere in the runner.
        public const string EnableEnvVar = "ACTIONS_RUNNER_ALLOW_ARTIFACTS_FILE";

        public Type ExtensionType => typeof(IFileCommandExtension);

        // Recognized scheme prefixes (case-insensitive).
        private const string FileScheme = "file://";
        private const string OciScheme = "oci://";

        // Matches "<scheme>://...". Used to detect unsupported URI schemes.
        // Scheme grammar per RFC 3986: ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )
        private static readonly Regex s_schemeRegex = new(
            @"^[A-Za-z][A-Za-z0-9+.\-]*://",
            RegexOptions.Compiled);

        // Matches "<ref>@<algo>:<hex>" where <algo> is sha256/sha384/sha512.
        // Hex length is validated separately so we can produce a precise error.
        private static readonly Regex s_ociDigestSuffixRegex = new(
            @"^(?<ref>.+)@(?<algo>sha(?:256|384|512)):(?<hex>[0-9a-fA-F]+)$",
            RegexOptions.Compiled);

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            ArgUtil.NotNull(context, nameof(context));

            // Feature flag gate. Enabled when either the server-side
            // feature flag is set, or the runner is started with the
            // ACTIONS_RUNNER_ALLOW_ARTIFACTS_FILE env var set to true
            // (the env-var fallback exists so self-hosted runners can
            // opt in locally). Silently no-op when disabled.
            var enabled = (context.Global.Variables.GetBoolean(Constants.Runner.Features.AllowArtifactsFile) ?? false)
                || StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable(EnableEnvVar));
            if (!enabled)
            {
                Trace.Verbose("$GITHUB_ARTIFACTS processing is disabled (feature flag and env-var fallback are both off).");
                return;
            }

            Trace.Info($"Processing $GITHUB_ARTIFACTS file '{filePath}'");

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Trace.Info("$GITHUB_ARTIFACTS file does not exist; nothing to process.");
                return;
            }

            var fileSize = new FileInfo(filePath).Length;
            if (fileSize == 0)
            {
                Trace.Info("$GITHUB_ARTIFACTS file is empty; nothing to process.");
                return;
            }
            if (fileSize > MaxFileSizeBytes)
            {
                throw new Exception(StringUtil.Format(
                    Constants.Runner.ArtifactsFileSizeExceeded,
                    MaxFileSizeBytes / 1024,
                    fileSize / 1024));
            }

            // Per-step subjects parsed from this file; aggregated into the
            // job-level set at the end so a single malformed line fails the
            // step without partially polluting the aggregate.
            var parsed = new List<(int LineNumber, ArtifactSubject Subject)>();

            // Relative artifact paths are resolved against the workspace
            // root (GITHUB_WORKSPACE), not the step's working directory.
            // This matches the established runner precedent set by
            // hashFiles() which always resolve relative paths against
            // the workspace root regardless of any step-level
            // `working-directory:`.
            var workspaceRoot = ResolveWorkspaceRoot(context);

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            for (var i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var raw = lines[i];
                var trimmed = raw.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                if (trimmed[0] == '#')
                {
                    continue;
                }

                ArtifactSubject subject;
                try
                {
                    subject = ParseLine(trimmed, workspaceRoot, container);
                }
                catch (ArtifactsParseException ex)
                {
                    throw new Exception(StringUtil.Format(
                        Constants.Runner.ArtifactsInvalidLine,
                        lineNumber,
                        ex.Message));
                }

                parsed.Add((lineNumber, subject));
            }

            // Aggregate at job scope: dedup identical, reject conflicts,
            // enforce the 500-artifact cap (after dedup so identical
            // duplicates above the cap do not fail).
            var aggregate = context.Global.ArtifactSubjects;
            if (aggregate == null)
            {
                throw new InvalidOperationException("Global.ArtifactSubjects is not initialized.");
            }

            var addedThisStep = 0;
            foreach (var (lineNumber, subject) in parsed)
            {
                if (aggregate.TryGetValue(subject.Name, out var existing))
                {
                    if (string.Equals(existing.Digest, subject.Digest, StringComparison.Ordinal))
                    {
                        // Identical declaration — silently deduplicate.
                        Trace.Info($"Skipped duplicate artifact subject '{subject.Name}' (digest={subject.Digest})");
                        continue;
                    }
                    throw new Exception(StringUtil.Format(
                        Constants.Runner.ArtifactsInvalidLine,
                        lineNumber,
                        StringUtil.Format(
                            Constants.Runner.ArtifactsConflictingDigest,
                            subject.Name,
                            existing.Digest,
                            subject.Digest)));
                }

                if (aggregate.Count >= MaxAggregateArtifacts)
                {
                    throw new Exception(StringUtil.Format(
                        Constants.Runner.ArtifactsInvalidLine,
                        lineNumber,
                        StringUtil.Format(
                            Constants.Runner.ArtifactsAggregateLimitExceeded,
                            MaxAggregateArtifacts)));
                }

                aggregate[subject.Name] = subject;
                addedThisStep++;
                Trace.Info($"Declared artifact subject '{subject.Name}' (kind={subject.Kind}, digest={subject.Digest})");
                context.Debug($"Declared artifact subject '{subject.Name}' (kind={subject.Kind}, digest={subject.Digest})");
            }

            if (addedThisStep > 0)
            {
                // Mirror the existing file-command UX: a single, terse
                // user-visible line that confirms the declarations landed.
                context.Output($"Captured {addedThisStep} artifact subject(s) from this step (job total: {aggregate.Count}).");
            }
        }

        private ArtifactSubject ParseLine(string trimmed, string workspaceRoot, ContainerInfo container)
        {
            // Reject lines containing '=' — reserved for a future v2
            // key/value extension to the format.
            if (trimmed.IndexOf('=') >= 0)
            {
                throw new ArtifactsParseException("entries containing '=' are reserved and not permitted");
            }

            // Handle the explicit escape-hatch schemes first
            // (case-insensitive).
            if (StartsWithIgnoreCase(trimmed, FileScheme))
            {
                var path = trimmed.Substring(FileScheme.Length);
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArtifactsParseException("file:// entries must include a path");
                }
                return MakeFileSubject(path, workspaceRoot, container);
            }
            if (StartsWithIgnoreCase(trimmed, OciScheme))
            {
                var rest = trimmed.Substring(OciScheme.Length);
                var match = s_ociDigestSuffixRegex.Match(rest);
                if (!match.Success)
                {
                    throw new ArtifactsParseException("oci:// entries must include an @sha{256,384,512}:<hex> digest");
                }
                return MakeOciSubject(match);
            }

            // Reject any other URI scheme up-front.
            if (s_schemeRegex.IsMatch(trimmed))
            {
                throw new ArtifactsParseException("unsupported URI scheme");
            }

            // Otherwise discriminate syntactically: an entry that matches
            // the OCI digest suffix shape (with the right hex length for
            // its algorithm) is an OCI subject; everything else is a path.
            var ociMatch = s_ociDigestSuffixRegex.Match(trimmed);
            if (ociMatch.Success && IsExpectedHexLength(ociMatch.Groups["algo"].Value, ociMatch.Groups["hex"].Value))
            {
                return MakeOciSubject(ociMatch);
            }

            return MakeFileSubject(trimmed, workspaceRoot, container);
        }

        private static ArtifactSubject MakeOciSubject(Match match)
        {
            var refName = match.Groups["ref"].Value;
            var algo = match.Groups["algo"].Value.ToLowerInvariant();
            var hex = match.Groups["hex"].Value.ToLowerInvariant();

            if (!IsExpectedHexLength(algo, hex))
            {
                throw new ArtifactsParseException(
                    $"digest '{algo}' must be {ExpectedHexLength(algo)} hex characters, got {hex.Length}");
            }
            if (string.IsNullOrEmpty(refName))
            {
                throw new ArtifactsParseException("oci subject must include a reference");
            }

            return new ArtifactSubject(refName, $"{algo}:{hex}", ArtifactSubjectKind.OciSubject);
        }

        private static ArtifactSubject MakeFileSubject(string declaredPath, string workspaceRoot, ContainerInfo container)
        {
            var hostPath = ResolveFilePath(declaredPath, workspaceRoot, container);

            if (!File.Exists(hostPath))
            {
                if (Directory.Exists(hostPath))
                {
                    throw new ArtifactsParseException($"'{declaredPath}' is a directory, not a regular file");
                }
                // For relative paths, surface where we looked so authors
                // aren't surprised that resolution is workspace-relative.
                if (!Path.IsPathRooted(declaredPath))
                {
                    throw new ArtifactsParseException(
                        $"file '{declaredPath}' does not exist (relative paths are resolved against the workspace root '{workspaceRoot}')");
                }
                throw new ArtifactsParseException($"file '{declaredPath}' does not exist");
            }

            // FileInfo + File.GetAttributes guards against named pipes,
            // device files, etc. We accept regular files and symlinks
            // resolved to regular files.
            var attrs = File.GetAttributes(hostPath);
            if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
            {
                throw new ArtifactsParseException($"'{declaredPath}' is a directory, not a regular file");
            }

            string hex;
            using (var stream = File.OpenRead(hostPath))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }
                hex = sb.ToString();
            }

            var name = Path.GetFileName(hostPath);
            return new ArtifactSubject(name, $"sha256:{hex}", ArtifactSubjectKind.File);
        }

        private static string ResolveFilePath(string declaredPath, string workspaceRoot, ContainerInfo container)
        {
            if (Path.IsPathRooted(declaredPath))
            {
                if (container == null)
                {
                    return declaredPath;
                }

                // Absolute path from a container step: it lives in the
                // container's filesystem namespace, so translate it to the
                // host path via the container's volume mounts.
                // TranslateToHostPath returns the input unchanged when the
                // path is not under any mount. We must NOT fall back to the
                // host file at that same path -- that would hash an arbitrary
                // host file the container step never referenced -- so reject
                // it instead.
                var hostPath = container.TranslateToHostPath(declaredPath);
                if (string.Equals(hostPath, declaredPath, StringComparison.Ordinal))
                {
                    throw new ArtifactsParseException(
                        $"absolute path '{declaredPath}' is not inside a volume mounted into the container and cannot be resolved");
                }
                return hostPath;
            }

            // Relative path: resolve against the workspace root
            // (GITHUB_WORKSPACE).
            var baseDir = workspaceRoot ?? string.Empty;
            return Path.GetFullPath(Path.Combine(baseDir, declaredPath));
        }

        private static string ResolveWorkspaceRoot(IExecutionContext context)
        {
            // The workspace root (GITHUB_WORKSPACE) is the resolution base
            // for all relative artifact paths.
            var workspace = context.GetGitHubContext("workspace");
            return string.IsNullOrEmpty(workspace) ? null : workspace;
        }

        private static bool StartsWithIgnoreCase(string s, string prefix)
        {
            return s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static int ExpectedHexLength(string algo)
        {
            return algo.ToLowerInvariant() switch
            {
                "sha256" => 64,
                "sha384" => 96,
                "sha512" => 128,
                _ => -1,
            };
        }

        private static bool IsExpectedHexLength(string algo, string hex)
        {
            var expected = ExpectedHexLength(algo);
            return expected > 0 && hex.Length == expected;
        }

        private sealed class ArtifactsParseException : Exception
        {
            public ArtifactsParseException(string message) : base(message) { }
        }
    }
}
