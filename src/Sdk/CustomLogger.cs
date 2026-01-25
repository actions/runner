using System;
using System.IO;

namespace GitHub.Runner.Sdk
{
    /// <summary>
    /// Custom logger for tracking executed commands
    /// Controlled by RUNNER_CUSTOM_LOG environment variable
    /// </summary>
    public static class CustomLogger
    {
        private static readonly object _lock = new object();
        private static bool? _isEnabled;
        private static string _logDir;

        public static bool IsEnabled
        {
            get
            {
                if (!_isEnabled.HasValue)
                {
                    _isEnabled = Environment.GetEnvironmentVariable("RUNNER_CUSTOM_LOG")?.ToLower() == "true";
                }
                return _isEnabled.Value;
            }
        }

        public static string LogDirectory
        {
            get
            {
                if (_logDir == null)
                {
                    _logDir = Environment.GetEnvironmentVariable("RUNNER_LOGGER_DIR");
                    if (string.IsNullOrEmpty(_logDir))
                    {
                        _logDir = Path.Combine(Directory.GetCurrentDirectory(), "_custom_logs");
                    }

                    if (IsEnabled && !Directory.Exists(_logDir))
                    {
                        try
                        {
                            Directory.CreateDirectory(_logDir);
                        }
                        catch
                        {
                            // Silently fail if directory creation fails
                        }
                    }
                }
                return _logDir;
            }
        }

        public static void LogCommand(string fileName, string arguments, string workingDirectory)
        {
            if (!IsEnabled)
            {
                return;
            }

            try
            {
                // Determine command type based on file name
                string commandType = "unknown";
                string baseName = Path.GetFileName(fileName).ToLower();

                if (baseName.Contains("bash") || baseName.Contains("sh"))
                    commandType = "bash";
                else if (baseName.Contains("node"))
                    commandType = "nodejs";
                else if (baseName.Contains("python"))
                    commandType = "python";
                else if (baseName.Contains("docker") || baseName.Contains("container"))
                    commandType = "docker";
                else if (baseName.Contains("dotnet") || baseName.EndsWith(".dll") || baseName.EndsWith(".exe"))
                    commandType = "dotnet";

                string logFile = Path.Combine(LogDirectory, "executed-commands.log");
                string timestamp = DateTime.UtcNow.ToString("O");
                string command = string.IsNullOrEmpty(arguments) ? fileName : $"{fileName} {arguments}";
                string logLine = $"[{timestamp}] TYPE={commandType} | CMD={command} | CWD={workingDirectory ?? ""}\n";

                lock (_lock)
                {
                    File.AppendAllText(logFile, logLine);
                }

                // Copy bash script files to temp_bash folder
                if (commandType == "bash" && !string.IsNullOrEmpty(arguments))
                {
                    CopyBashScript(arguments);
                }
            }
            catch
            {
                // Silently fail - don't break runner if custom logging fails
            }
        }

        private static void CopyBashScript(string arguments)
        {
            try
            {
                // Parse arguments to find the script file path
                // Bash commands typically look like: --noprofile --norc -e -o pipefail /path/to/script.sh
                string scriptPath = null;

                // Split arguments and find the .sh file
                var argParts = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in argParts)
                {
                    if (part.EndsWith(".sh") && File.Exists(part))
                    {
                        scriptPath = part;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(scriptPath))
                {
                    return;
                }

                // Create temp_bash directory
                string tempBashDir = Path.Combine(LogDirectory, "temp_bash");
                if (!Directory.Exists(tempBashDir))
                {
                    Directory.CreateDirectory(tempBashDir);
                }

                // Copy the script file, preserving the filename
                string scriptFileName = Path.GetFileName(scriptPath);
                string destPath = Path.Combine(tempBashDir, scriptFileName);

                // If file already exists, append timestamp to make it unique
                if (File.Exists(destPath))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(scriptFileName);
                    string ext = Path.GetExtension(scriptFileName);
                    string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    destPath = Path.Combine(tempBashDir, $"{fileNameWithoutExt}_{timestamp}{ext}");
                }

                File.Copy(scriptPath, destPath, false);
            }
            catch
            {
                // Silently fail - don't break runner if script copy fails
            }
        }
    }
}
