using GitHub.Runner.Worker.Container;
using System;
using System.Text;
using System.IO;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Collections;
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(FileCommandManager))]
    public interface IFileCommandManager : IRunnerService
    {
        void InitializeFiles(IExecutionContext context, ContainerInfo container);
        void TryProcessFiles(IExecutionContext context, ContainerInfo container);
    
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
                var oldPath = Path.Combine(_fileCommandDirectory, fileCommand.FileName + oldSuffix);
                if (oldSuffix != String.Empty && File.Exists(oldPath))
                {
                    TryDeleteFile(oldPath);
                }

                var newPath = Path.Combine(_fileCommandDirectory, fileCommand.FileName + _fileSuffix);
                TryDeleteFile(newPath);
                File.Create(newPath).Dispose();

                var pathToSet = container != null ? container.TranslateToContainerPath(newPath) : newPath; 
                context.SetGitHubContext(fileCommand.ContextName, pathToSet);
            }
        }

        public void TryProcessFiles(IExecutionContext context, ContainerInfo container)
        {
            foreach (var fileCommand in _commandExtensions)
            {
                fileCommand.ProcessCommand(context, Path.Combine(_fileCommandDirectory, fileCommand.FileName + _fileSuffix),container);
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
        string FileName { get; }

        void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container);
    }

    public sealed class AddPathFileCommand : RunnerService, IFileCommandExtension
    {
        public string ContextName => "path";
        public string FileName => "add_path_";

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
        public string FileName => "set_env_";

        public Type ExtensionType => typeof(IFileCommandExtension);

        public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
        {
            if (File.Exists(filePath))
            {
                // TODO Process this file
            }
        }
    }
}
