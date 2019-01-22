using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(PowerShellExeHandler))]
    public interface IPowerShellExeHandler : IHandler
    {
        PowerShellExeHandlerData Data { get; set; }

        string AccessToken { get; set; }
    }

    public sealed class PowerShellExeHandler : Handler, IPowerShellExeHandler
    {
        private const string InlineScriptType = "inlineScript";
        private readonly object _outputLock = new object();
        private readonly StringBuilder _errorBuffer = new StringBuilder();
        private volatile int _errorCount;
        private bool _failOnStandardError;
        public PowerShellExeHandlerData Data { get; set; }
        public string AccessToken { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.Directory(TaskDirectory, nameof(TaskDirectory));

            // Update the env dictionary.
            AddVariablesToEnvironment(excludeNames: true, excludeSecrets: true);
            AddPrependPathToEnvironment();

            // Add the access token to the environment variables, if the access token is set.
            if (!string.IsNullOrEmpty(AccessToken))
            {
                string formattedKey = Constants.Variables.System.AccessToken.Replace('.', '_').Replace(' ', '_').ToUpperInvariant();
                AddEnvironmentVariable(formattedKey, AccessToken);
            }

            // Determine whether to fail on STDERR.
            _failOnStandardError = StringUtil.ConvertToBoolean(Data.FailOnStandardError, true); // Default to true.

            // Get the script file.
            string scriptFile = null;
            try
            {
                if (string.Equals(Data.ScriptType, InlineScriptType, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Write this file under the _work folder and clean it up at the beginning of the next build?
                    // Write the inline script to a temp file.
                    string tempDirectory = Path.GetTempPath();
                    ArgUtil.Directory(tempDirectory, nameof(tempDirectory));
                    scriptFile = Path.Combine(tempDirectory, $"{Guid.NewGuid()}.ps1");
                    Trace.Info("Writing inline script to temp file: '{0}'", scriptFile);
                    File.WriteAllText(scriptFile, Data.InlineScript ?? string.Empty, Encoding.UTF8);
                }
                else
                {
                    // TODO: If not rooted, WHICH the file if it doesn't contain any slashes.
                    // Assert the target file.
                    ArgUtil.NotNullOrEmpty(Data.Target, nameof(Data.Target));
                    scriptFile = Data.Target;
                }

                // Define the nested expression to invoke the user-specified script file and arguments.
                // Use the dot operator (".") to run the script in the same scope.
                string nestedExpression = StringUtil.Format(
                    ". '{0}' {1}",
                    scriptFile.Trim('"').Replace("'", "''"),
                    Data.ArgumentFormat);

                // Craft the args to pass to PowerShell.exe. The user-defined expression is jammed in
                // as an encrypted base 64 string to a wrapper command. This solves a couple problems:
                // 1) Avoids quoting issues by jamming all of the user input into a base-64 encoded.
                // 2) Handles setting the exit code.
                //
                // The goal here is to jam everything into a base 64 encoded string so that quoting
                // issues can be avoided. The data needs to be encrypted because base 64 encoding the
                // data circumvents the logger's secret-masking behavior.
                string entropy;
                string powerShellExeArgs = StringUtil.Format(
                    "-NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command \"try {{ $null = [System.Security.Cryptography.ProtectedData] }} catch {{ Write-Verbose 'Adding assemly: System.Security' ; Add-Type -AssemblyName 'System.Security' ; $null = [System.Security.Cryptography.ProtectedData] ; $Error.Clear() }} ; Invoke-Expression -Command ([System.Text.Encoding]::UTF8.GetString([System.Security.Cryptography.ProtectedData]::Unprotect([System.Convert]::FromBase64String('{0}'), [System.Convert]::FromBase64String('{1}'), [System.Security.Cryptography.DataProtectionScope]::CurrentUser))) ; if (!(Test-Path -LiteralPath variable:\\LastExitCode)) {{ Write-Verbose 'Last exit code is not set.' }} else {{ Write-Verbose ('$LastExitCode: {{0}}' -f $LastExitCode) ; exit $LastExitCode }}\"",
                    Encrypt(nestedExpression, out entropy),
                    entropy);

                // Resolve powershell.exe.
                string powerShellExe = HostContext.GetService<IPowerShellExeUtil>().GetPath();
                ArgUtil.NotNullOrEmpty(powerShellExe, nameof(powerShellExe));

                // Determine whether the script file is rooted.
                // TODO: If script file begins and ends with a double-quote, trim quotes before making determination. Likewise when determining whether the file exists.
                bool isScriptFileRooted = false;
                try
                {
                    // Path.IsPathRooted throws if illegal characters are in the path.
                    isScriptFileRooted = Path.IsPathRooted(scriptFile);
                }
                catch (Exception ex)
                {
                    Trace.Info($"Unable to determine whether the script file is rooted: {ex.Message}");
                }

                Trace.Info($"Script file is rooted: {isScriptFileRooted}");

                // Determine the working directory.
                string workingDirectory;
                if (!string.IsNullOrEmpty(Data.WorkingDirectory))
                {
                    workingDirectory = Data.WorkingDirectory;
                }
                else
                {
                    if (isScriptFileRooted && File.Exists(scriptFile))
                    {
                        workingDirectory = Path.GetDirectoryName(scriptFile);
                    }
                    else
                    {
                        workingDirectory = Path.Combine(TaskDirectory, "DefaultTaskWorkingDirectory");
                    }
                }

                ExecutionContext.Debug($"Working directory: '{workingDirectory}'");
                Directory.CreateDirectory(workingDirectory);

                // Invoke the process.
                ExecutionContext.Debug($"{powerShellExe} {powerShellExeArgs}");
                ExecutionContext.Command(nestedExpression);
                using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
                {
                    processInvoker.OutputDataReceived += OnOutputDataReceived;
                    processInvoker.ErrorDataReceived += OnErrorDataReceived;
                    int exitCode = await processInvoker.ExecuteAsync(workingDirectory: workingDirectory,
                                                                     fileName: powerShellExe,
                                                                     arguments: powerShellExeArgs,
                                                                     environment: Environment,
                                                                     requireExitCodeZero: false,
                                                                     outputEncoding: null,
                                                                     killProcessOnCancel: false,
                                                                     redirectStandardIn: null,
                                                                     inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                                                     cancellationToken: ExecutionContext.CancellationToken);
                    FlushErrorData();

                    // Fail on error count.
                    if (_failOnStandardError && _errorCount > 0)
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
            finally
            {
                try
                {
                    if (string.Equals(Data.ScriptType, InlineScriptType, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(scriptFile) &&
                        File.Exists(scriptFile))
                    {
                        File.Delete(scriptFile);
                    }
                }
                catch (Exception ex)
                {
                    ExecutionContext.Warning(StringUtil.Loc("FailedToDeleteTempScript", scriptFile, ex.Message));
                    Trace.Error(ex);
                }
            }
        }

        private static String Encrypt(String str, out String entropy)
        {
            byte[] entropyBytes = new byte[16];
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(entropyBytes);
            }

            entropy = Convert.ToBase64String(entropyBytes);
            byte[] protectedBytes = ProtectedData.Protect(
                userData: Encoding.UTF8.GetBytes(str),
                optionalEntropy: entropyBytes,
                scope: DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
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
                if (_failOnStandardError)
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _errorBuffer.AppendLine(e.Data);
                    }
                }
                else
                {
                    ExecutionContext.Output(e.Data);
                }
            }
        }

        private void OnOutputDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            lock (_outputLock)
            {
                FlushErrorData();
                if (!CommandManager.TryProcessCommand(ExecutionContext, e.Data))
                {
                    ExecutionContext.Output(e.Data);
                }
            }
        }
    }
}
