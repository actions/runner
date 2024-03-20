using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(FileCommandManager))]
    public interface IFileCommandManager : IRunnerService
    {
        void InitializeFiles(IExecutionContext context, ContainerInfo container);
        void ProcessFiles(IExecutionContext context, ContainerInfo container);

    }

    public sealed class FileCommandManager : RunnerService, IFileCommandManager
    {
        private const string _folderName = "_runner_file_commands";
        private List<IFileCommandExtension> _commandExtensions;
        private string _fileSuffix = String.Empty;
        private string _fileCommandDirectory;
        private Tracing _trace;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _trace = HostContext.GetTrace(nameof(FileCommandManager));

            _fileCommandDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), _folderName);
            if (!Directory.Exists(_fileCommandDirectory))
            {
                Directory.CreateDirectory(_fileCommandDirectory);
            }

            var extensionManager = hostContext.GetService<IExtensionManager>();
            _commandExtensions = extensionManager.GetExtensions<IFileCommandExtension>() ?? new List<IFileCommandExtension>();
        }

        public void InitializeFiles(IExecutionContext context, ContainerInfo container)
        {
            var oldSuffix = _fileSuffix;
            _fileSuffix = Guid.NewGuid().ToString();
            foreach (var fileCommand in _commandExtensions)
            {
                var oldPath = Path.Combine(_fileCommandDirectory, fileCommand.FilePrefix + oldSuffix);
                if (oldSuffix != String.Empty && File.Exists(oldPath))
                {
                    TryDeleteFile(oldPath);
                }

                var newPath = Path.Combine(_fileCommandDirectory, fileCommand.FilePrefix + _fileSuffix);
                TryDeleteFile(newPath);
                File.Create(newPath).Dispose();

                var pathToSet = container != null ? container.TranslateToContainerPath(newPath) : newPath;
                context.SetGitHubContext(fileCommand.ContextName, pathToSet);
            }
        }

        public void ProcessFiles(IExecutionContext context, ContainerInfo container)
        {
            foreach (var fileCommand in _commandExtensions)
            {
                try
                {
                    fileCommand.ProcessCommand(context, Path.Combine(_fileCommandDirectory, fileCommand.FilePrefix + _fileSuffix), container);
                }
                catch (Exception ex)
                {
                    context.Error($"Unable to process file command '{fileCommand.ContextName}' successfully.");
                    context.Error(ex);
                    context.CommandResult = TaskResult.Failed;
                }
            }
        }

        private bool TryDeleteFile(string path)
        {
            if (!File.Exists(path))
            {
                return true;
            }
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                _trace.Warning($"Unable to delete file {path} for reason: {e.ToString()}");
                return false;
            }
            return true;
        }
    }

    public interface IFileCommandExtension : IExtension
    {
        string ContextName { get; }
        string FilePrefix { get; }

        void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container);
    }

    public sealed class AddPathFileCommand : RunnerService, IFileCommandExtension
    {
        public string ContextName => "path";
        public string FilePrefix => "add_path_";

        public Type ExtensionType => typeof(IFileCommandExtension);

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (line == string.Empty)
                    {
                        continue;
                    }
                    context.Global.PrependPath.RemoveAll(x => string.Equals(x, line, StringComparison.CurrentCulture));
                    context.Global.PrependPath.Add(line);
                }
            }
        }
    }

    public sealed class SetEnvFileCommand : RunnerService, IFileCommandExtension
    {
        public string ContextName => "env";
        public string FilePrefix => "set_env_";

        public Type ExtensionType => typeof(IFileCommandExtension);

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            var pairs = new EnvFileKeyValuePairs(context, filePath);
            foreach (var pair in pairs)
            {
                var isBlocked = false;
                foreach (var blocked in _setEnvBlockList)
                {
                    if (string.Equals(blocked, pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        // Log Telemetry and let user know they shouldn't do this
                        var issue = new Issue()
                        {
                            Type = IssueType.Error,
                            Message = $"Can't store {blocked} output parameter using '$GITHUB_ENV' command."
                        };
                        issue.Data[Constants.Runner.InternalTelemetryIssueDataKey] = $"{Constants.Runner.UnsupportedCommand}_{pair.Key}";
                        context.AddIssue(issue, ExecutionContextLogOptions.Default);

                        isBlocked = true;
                        break;
                    }
                }
                if (isBlocked)
                {
                    continue;
                }
                SetEnvironmentVariable(context, pair.Key, pair.Value);
            }
        }

        private static void SetEnvironmentVariable(
            IExecutionContext context,
            string name,
            string value)
        {
            context.Global.EnvironmentVariables[name] = value;
            context.SetEnvContext(name, value);
            context.Debug($"{name}='{value}'");
        }

        private string[] _setEnvBlockList =
        {
            "NODE_OPTIONS"
        };
    }

    public sealed class CreateStepSummaryCommand : RunnerService, IFileCommandExtension
    {
        public const int AttachmentSizeLimit = 1024 * 1024;

        public string ContextName => "step_summary";
        public string FilePrefix => "step_summary_";

        public Type ExtensionType => typeof(IFileCommandExtension);

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            if (String.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Trace.Info($"Step Summary file ({filePath}) does not exist; skipping attachment upload");
                return;
            }

            try
            {
                var fileSize = new FileInfo(filePath).Length;
                if (fileSize == 0)
                {
                    Trace.Info($"Step Summary file ({filePath}) is empty; skipping attachment upload");
                    return;
                }

                if (fileSize > AttachmentSizeLimit)
                {
                    context.Error(String.Format(Constants.Runner.UnsupportedSummarySize, AttachmentSizeLimit / 1024, fileSize / 1024));
                    Trace.Info($"Step Summary file ({filePath}) is too large ({fileSize} bytes); skipping attachment upload");

                    return;
                }

                Trace.Verbose($"Step Summary file exists: {filePath} and has a file size of {fileSize} bytes");
                var scrubbedFilePath = filePath + "-scrubbed";

                using (var streamReader = new StreamReader(filePath))
                using (var streamWriter = new StreamWriter(scrubbedFilePath))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        var maskedLine = HostContext.SecretMasker.MaskSecrets(line);
                        streamWriter.WriteLine(maskedLine);
                    }
                }

                var attachmentName = !context.IsEmbedded
                    ? context.Id.ToString()
                    : context.EmbeddedId.ToString();

                Trace.Info($"Queueing file ({filePath}) for attachment upload ({attachmentName})");
                // Attachments must be added to the parent context (job), not the current context (step)
                context.Root.QueueAttachFile(ChecksAttachmentType.StepSummary, attachmentName, scrubbedFilePath);

                // Dual upload the same files to Results Service
                context.Global.Variables.TryGetValue("system.github.results_endpoint", out string resultsReceiverEndpoint);
                if (resultsReceiverEndpoint != null)
                {
                    Trace.Info($"Queueing results file ({filePath}) for attachment upload ({attachmentName})");
                    var stepId = context.IsEmbedded ? context.EmbeddedId : context.Id;
                    // Attachments must be added to the parent context (job), not the current context (step)
                    context.Root.QueueSummaryFile(attachmentName, scrubbedFilePath, stepId);
                }
            }
            catch (Exception e)
            {
                Trace.Error($"Error while processing file ({filePath}): {e}");
                context.Error($"Failed to create step summary using 'GITHUB_STEP_SUMMARY': {e.Message}");
            }
        }
    }

    public sealed class SaveStateFileCommand : RunnerService, IFileCommandExtension
    {
        public string ContextName => "state";
        public string FilePrefix => "save_state_";

        public Type ExtensionType => typeof(IFileCommandExtension);

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            var pairs = new EnvFileKeyValuePairs(context, filePath);
            foreach (var pair in pairs)
            {
                // Embedded steps (composite) keep track of the state at the root level
                if (context.IsEmbedded)
                {
                    var id = context.EmbeddedId;
                    if (!context.Root.EmbeddedIntraActionState.ContainsKey(id))
                    {
                        context.Root.EmbeddedIntraActionState[id] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    context.Root.EmbeddedIntraActionState[id][pair.Key] = pair.Value;
                }
                // Otherwise modify the ExecutionContext
                else
                {
                    context.IntraActionState[pair.Key] = pair.Value;
                }

                context.Debug($"Save intra-action state {pair.Key} = {pair.Value}");
            }
        }
    }

    public sealed class SetOutputFileCommand : RunnerService, IFileCommandExtension
    {
        public string ContextName => "output";
        public string FilePrefix => "set_output_";

        public Type ExtensionType => typeof(IFileCommandExtension);

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            var pairs = new EnvFileKeyValuePairs(context, filePath);
            foreach (var pair in pairs)
            {
                context.SetOutput(pair.Key, pair.Value, out var reference);
                context.Debug($"Set output {pair.Key} = {pair.Value}");
            }
        }
    }

    public sealed class EnvFileKeyValuePairs : IEnumerable<KeyValuePair<string, string>>
    {
        private IExecutionContext _context;
        private string _filePath;

        public EnvFileKeyValuePairs(IExecutionContext context, string filePath)
        {
            _context = context;
            _filePath = filePath;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            var text = string.Empty;
            try
            {
                text = File.ReadAllText(_filePath) ?? string.Empty;
            }
            catch (DirectoryNotFoundException)
            {
                _context.Debug($"File does not exist '{_filePath}'");
                yield break;
            }
            catch (FileNotFoundException)
            {
                _context.Debug($"File does not exist '{_filePath}'");
                yield break;
            }

            var index = 0;
            var line = ReadLine(text, ref index);
            while (line != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var key = string.Empty;
                    var output = string.Empty;

                    var equalsIndex = line.IndexOf("=", StringComparison.Ordinal);
                    var heredocIndex = line.IndexOf("<<", StringComparison.Ordinal);

                    // Normal style NAME=VALUE
                    if (equalsIndex >= 0 && (heredocIndex < 0 || equalsIndex < heredocIndex))
                    {
                        var split = line.Split(new[] { '=' }, 2, StringSplitOptions.None);
                        if (string.IsNullOrEmpty(line))
                        {
                            throw new Exception($"Invalid format '{line}'. Name must not be empty");
                        }

                        key = split[0];
                        output = split[1];
                    }

                    // Heredoc style NAME<<EOF
                    else if (heredocIndex >= 0 && (equalsIndex < 0 || heredocIndex < equalsIndex))
                    {
                        var split = line.Split(new[] { "<<" }, 2, StringSplitOptions.None);
                        if (string.IsNullOrEmpty(split[0]) || string.IsNullOrEmpty(split[1]))
                        {
                            throw new Exception($"Invalid format '{line}'. Name must not be empty and delimiter must not be empty");
                        }
                        key = split[0];
                        var delimiter = split[1];
                        var startIndex = index; // Start index of the value (inclusive)
                        var endIndex = index;   // End index of the value (exclusive)
                        var tempLine = ReadLine(text, ref index, out var newline);
                        while (!string.Equals(tempLine, delimiter, StringComparison.Ordinal))
                        {
                            if (tempLine == null)
                            {
                                throw new Exception($"Invalid value. Matching delimiter not found '{delimiter}'");
                            }
                            if (newline == null)
                            {
                                throw new Exception($"Invalid value. EOF marker missing new line.");
                            }
                            endIndex = index - newline.Length;
                            tempLine = ReadLine(text, ref index, out newline);
                        }

                        output = endIndex > startIndex ? text.Substring(startIndex, endIndex - startIndex) : string.Empty;
                    }
                    else
                    {
                        throw new Exception($"Invalid format '{line}'");
                    }

                    yield return new KeyValuePair<string, string>(key, output);
                }

                line = ReadLine(text, ref index);
            }
        }

        System.Collections.IEnumerator
            System.Collections.IEnumerable.GetEnumerator()
        {
            // Invoke IEnumerator<KeyValuePair<string, string>> GetEnumerator() above.
            return GetEnumerator();
        }

        private static string ReadLine(
                string text,
                ref int index)
        {
            return ReadLine(text, ref index, out _);
        }

        private static string ReadLine(
                string text,
                ref int index,
                out string newline)
        {
            if (index >= text.Length)
            {
                newline = null;
                return null;
            }

            var originalIndex = index;
            var lfIndex = text.IndexOf("\n", index, StringComparison.Ordinal);
            if (lfIndex < 0)
            {
                index = text.Length;
                newline = null;
                return text.Substring(originalIndex);
            }

#if OS_WINDOWS
            var crLFIndex = text.IndexOf("\r\n", index, StringComparison.Ordinal);
            if (crLFIndex >= 0 && crLFIndex < lfIndex)
            {
                index = crLFIndex + 2; // Skip over CRLF
                newline = "\r\n";
                return text.Substring(originalIndex, crLFIndex - originalIndex);
            }
#endif

            index = lfIndex + 1; // Skip over LF
            newline = "\n";
            return text.Substring(originalIndex, lfIndex - originalIndex);
        }
    }
}
