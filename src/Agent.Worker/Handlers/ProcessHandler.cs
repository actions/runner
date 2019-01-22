using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ProcessHandler))]
    public interface IProcessHandler : IHandler
    {
        ProcessHandlerData Data { get; set; }
    }

    public sealed class ProcessHandler : Handler, IProcessHandler
    {
        private const string OutputDelimiter = "##ENV_DELIMITER_d8c0672b##";
        private readonly object _outputLock = new object();
        private readonly StringBuilder _errorBuffer = new StringBuilder();
        private volatile int _errorCount;
        private bool _foundDelimiter;
        private bool _modifyEnvironment;

        public ProcessHandlerData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.NotNull(TaskDirectory, nameof(TaskDirectory));

            // Update the env dictionary.
            AddVariablesToEnvironment(excludeNames: true, excludeSecrets: true);
            AddPrependPathToEnvironment();

            // Get the command.
            ArgUtil.NotNullOrEmpty(Data.Target, nameof(Data.Target));
            // TODO: WHICH the command?
            string command = Data.Target;

            // Determine whether the command is rooted.
            // TODO: If command begins and ends with a double-quote, trim quotes before making determination. Likewise when determining whether the file exists.
            bool isCommandRooted = false;
            try
            {
                // Path.IsPathRooted throws if illegal characters are in the path.
                isCommandRooted = Path.IsPathRooted(command);
            }
            catch (Exception ex)
            {
                Trace.Info($"Unable to determine whether the command is rooted: {ex.Message}");
            }

            Trace.Info($"Command is rooted: {isCommandRooted}");

            // Determine the working directory.
            string workingDirectory;
            if (!string.IsNullOrEmpty(Data.WorkingDirectory))
            {
                workingDirectory = Data.WorkingDirectory;
            }
            else
            {
                if (isCommandRooted && File.Exists(command))
                {
                    workingDirectory = Path.GetDirectoryName(command);
                }
                else
                {
                    workingDirectory = Path.Combine(TaskDirectory, "DefaultTaskWorkingDirectory");
                }
            }

            ExecutionContext.Debug($"Working directory: '{workingDirectory}'");
            Directory.CreateDirectory(workingDirectory);

            // Wrap the command in quotes if required.
            //
            // This is guess-work but is probably mostly accurate. The problem is that the command text
            // box is separate from the args text box. This implies to the user that we take care of quoting
            // for the command.
            //
            // The approach taken here is only to quote if it needs quotes. We should stay out of the way
            // as much as possible. Built-in shell commands will not work if they are quoted, e.g. RMDIR.
            if (command.Contains(" ") || command.Contains("%"))
            {
                if (!command.Contains("\""))
                {
                    command = StringUtil.Format("\"{0}\"", command);
                }
            }

            // Get the arguments.
            string arguments = Data.ArgumentFormat ?? string.Empty;

            // Get the fail on standard error flag.
            bool failOnStandardError = true;
            string failOnStandardErrorString;
            if (Inputs.TryGetValue("failOnStandardError", out failOnStandardErrorString))
            {
                failOnStandardError = StringUtil.ConvertToBoolean(failOnStandardErrorString);
            }

            ExecutionContext.Debug($"Fail on standard error: '{failOnStandardError}'");

            // Get the modify environment flag.
            _modifyEnvironment = StringUtil.ConvertToBoolean(Data.ModifyEnvironment);
            ExecutionContext.Debug($"Modify environment: '{_modifyEnvironment}'");

            // Resolve cmd.exe.
            string cmdExe = System.Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrEmpty(cmdExe))
            {
                cmdExe = "cmd.exe";
            }

            // Format the input to be invoked from cmd.exe to enable built-in shell commands. For example, RMDIR.
            string cmdExeArgs;
            if (_modifyEnvironment)
            {
                // Format the command so the environment variables can be captured.
                cmdExeArgs = $"/c \"{command} {arguments} && echo {OutputDelimiter} && set \"";
            }
            else
            {
                cmdExeArgs = $"/c \"{command} {arguments}\"";
            }

            // Invoke the process.
            ExecutionContext.Debug($"{cmdExe} {cmdExeArgs}");
            ExecutionContext.Command($"{command} {arguments}");
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += OnOutputDataReceived;
                if (failOnStandardError)
                {
                    processInvoker.ErrorDataReceived += OnErrorDataReceived;
                }
                else
                {
                    processInvoker.ErrorDataReceived += OnOutputDataReceived;
                }

                int exitCode = await processInvoker.ExecuteAsync(workingDirectory: workingDirectory,
                                                                 fileName: cmdExe,
                                                                 arguments: cmdExeArgs,
                                                                 environment: Environment,
                                                                 requireExitCodeZero: false,
                                                                 outputEncoding: null,
                                                                 killProcessOnCancel: false,
                                                                 redirectStandardIn: null,
                                                                 inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                                                 cancellationToken: ExecutionContext.CancellationToken);
                FlushErrorData();

                // Fail on error count.
                if (_errorCount > 0)
                {
                    if (ExecutionContext.Result != null)
                    {
                        Trace.Info($"Task result already set. Not failing due to error count ({_errorCount}).");
                    }
                    else
                    {
                        throw new Exception(StringUtil.Loc("ProcessCompletedWithCode0Errors1", exitCode, _errorCount));
                    }
                }

                // Fail on non-zero exit code.
                if (exitCode != 0)
                {
                    throw new Exception(StringUtil.Loc("ProcessCompletedWithExitCode0", exitCode));
                }
            }
        }

        private void FlushErrorData()
        {
            if (_errorBuffer.Length > 0)
            {
                ExecutionContext.Error(_errorBuffer.ToString());
                _errorCount++;
                _errorBuffer.Clear();
            }
        }

        private void OnErrorDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            lock (_outputLock)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _errorBuffer.AppendLine(e.Data);
                }
            }
        }

        private void OnOutputDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            lock (_outputLock)
            {
                FlushErrorData();
                string line = e.Data ?? string.Empty;
                if (_modifyEnvironment)
                {
                    if (_foundDelimiter)
                    {
                        // The line is output from the SET command. Update the environment.
                        int index = line.IndexOf('=');
                        if (index > 0)
                        {
                            string key = line.Substring(0, index);
                            string value = line.Substring(index + 1);

                            // Omit special environment variables:
                            //   "TF_BUILD" is set by ProcessInvoker.
                            //   "agent.jobstatus" is set by ???.
                            if (string.Equals(key, Constants.TFBuild, StringComparison.Ordinal)
                                || string.Equals(key, Constants.Variables.Agent.JobStatus, StringComparison.Ordinal))
                            {
                                return;
                            }

                            ExecutionContext.Debug($"Setting env '{key}' = '{value}'");
                            System.Environment.SetEnvironmentVariable(key, value);
                        }

                        return;
                    } // if (_foundDelimiter)

                    // Use StartsWith() instead of Equals() to allow for trailing spaces from the ECHO command.
                    if (line.StartsWith(OutputDelimiter, StringComparison.Ordinal))
                    {
                        // The line is the output delimiter.
                        // Set the flag and clear the environment variable dictionary.
                        _foundDelimiter = true;
                        return;
                    }
                } // if (_modifyEnvironment)

                // The line is output from the process that was invoked.
                if (!CommandManager.TryProcessCommand(ExecutionContext, line))
                {
                    ExecutionContext.Output(line);
                }
            }
        }
    }
}
