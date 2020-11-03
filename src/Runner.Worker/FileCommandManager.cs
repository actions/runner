using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System;
using System.Collections;
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
                    fileCommand.ProcessCommand(context, Path.Combine(_fileCommandDirectory, fileCommand.FilePrefix + _fileSuffix),container);
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
                foreach(var line in lines)
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
            try
            {
                var text = File.ReadAllText(filePath) ?? string.Empty;
                var index = 0;
                var line = ReadLine(text, ref index);
                while (line != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        var equalsIndex = line.IndexOf("=", StringComparison.Ordinal);
                        var heredocIndex = line.IndexOf("<<", StringComparison.Ordinal);

                        // Normal style NAME=VALUE
                        if (equalsIndex >= 0 && (heredocIndex < 0 || equalsIndex < heredocIndex))
                        {
                            var split = line.Split(new[] { '=' }, 2, StringSplitOptions.None);
                            if (string.IsNullOrEmpty(line))
                            {
                                throw new Exception($"Invalid environment variable format '{line}'. Environment variable name must not be empty");
                            }
                            SetEnvironmentVariable(context, split[0], split[1]);
                        }
                        // Heredoc style NAME<<EOF
                        else if (heredocIndex >= 0 && (equalsIndex < 0 || heredocIndex < equalsIndex))
                        {
                            var split = line.Split(new[] { "<<" }, 2, StringSplitOptions.None);
                            if (string.IsNullOrEmpty(split[0]) || string.IsNullOrEmpty(split[1]))
                            {
                                throw new Exception($"Invalid environment variable format '{line}'. Environment variable name must not be empty and delimiter must not be empty");
                            }
                            var name = split[0];
                            var delimiter = split[1];
                            var startIndex = index; // Start index of the value (inclusive)
                            var endIndex = index;   // End index of the value (exclusive)
                            var tempLine = ReadLine(text, ref index, out var newline);
                            while (!string.Equals(tempLine, delimiter, StringComparison.Ordinal))
                            {
                                if (tempLine == null)
                                {
                                    throw new Exception($"Invalid environment variable value. Matching delimiter not found '{delimiter}'");
                                }
                                endIndex = index - newline.Length;
                                tempLine = ReadLine(text, ref index, out newline);
                            }

                            var value = endIndex > startIndex ? text.Substring(startIndex, endIndex - startIndex) : string.Empty;
                            SetEnvironmentVariable(context, name, value);
                        }
                        else
                        {
                            throw new Exception($"Invalid environment variable format '{line}'");
                        }
                    }

                    line = ReadLine(text, ref index);
                }
            }
            catch (DirectoryNotFoundException)
            {
                context.Debug($"Environment variables file does not exist '{filePath}'");
            }
            catch (FileNotFoundException)
            {
                context.Debug($"Environment variables file does not exist '{filePath}'");
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
