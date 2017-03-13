using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(PowerShell3Handler))]
    public interface IPowerShell3Handler : IHandler
    {
        PowerShell3HandlerData Data { get; set; }
    }

    public sealed class PowerShell3Handler : Handler, IPowerShell3Handler
    {
        public PowerShell3HandlerData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.Directory(TaskDirectory, nameof(TaskDirectory));

            // Update the env dictionary.
            AddInputsToEnvironment();
            AddEndpointsToEnvironment();
            AddSecureFilesToEnvironment();
            AddVariablesToEnvironment();
            AddIntraTaskStatesToEnvironment();

            // Resolve the target script.
            ArgUtil.NotNullOrEmpty(Data.Target, nameof(Data.Target));
            string scriptFile = Path.Combine(TaskDirectory, Data.Target);
            ArgUtil.File(scriptFile, nameof(scriptFile));

            // Resolve the VSTS Task SDK module definition.
            string scriptDirectory = Path.GetDirectoryName(scriptFile);
            string moduleFile = Path.Combine(scriptDirectory, @"ps_modules", "VstsTaskSdk", "VstsTaskSdk.psd1");
            ArgUtil.File(moduleFile, nameof(moduleFile));

            // Craft the args to pass to PowerShell.exe.
            string powerShellExeArgs = StringUtil.Format(
                @"-NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "". ([scriptblock]::Create('if (!$PSHOME) {{ $null = Get-Item -LiteralPath ''variable:PSHOME'' }} else {{ Import-Module -Name ([System.IO.Path]::Combine($PSHOME, ''Modules\Microsoft.PowerShell.Management\Microsoft.PowerShell.Management.psd1'')) ; Import-Module -Name ([System.IO.Path]::Combine($PSHOME, ''Modules\Microsoft.PowerShell.Utility\Microsoft.PowerShell.Utility.psd1'')) }}')) 2>&1 | ForEach-Object {{ Write-Verbose $_.Exception.Message -Verbose }} ; Import-Module -Name '{0}' -ArgumentList @{{ NonInteractive = $true }} -ErrorAction Stop ; $VerbosePreference = '{1}' ; $DebugPreference = '{1}' ; Invoke-VstsTaskScript -ScriptBlock ([scriptblock]::Create('. ''{2}'''))""",
                moduleFile.Replace("'", "''"), // nested within a single-quoted string
                ExecutionContext.Variables.System_Debug == true ? "Continue" : "SilentlyContinue",
                scriptFile.Replace("'", "''''")); // nested within a single-quoted string within a single-quoted string

            // Resolve powershell.exe.
            string powerShellExe = HostContext.GetService<IPowerShellExeUtil>().GetPath();
            ArgUtil.NotNullOrEmpty(powerShellExe, nameof(powerShellExe));

            // Invoke the process.
            using (var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                processInvoker.OutputDataReceived += OnDataReceived;
                processInvoker.ErrorDataReceived += OnDataReceived;

                // Execute the process. Exit code 0 should always be returned.
                // A non-zero exit code indicates infrastructural failure.
                // Task failure should be communicated over STDOUT using ## commands.
                await processInvoker.ExecuteAsync(
                    workingDirectory: scriptDirectory,
                    fileName: powerShellExe,
                    arguments: powerShellExeArgs,
                    environment: Environment,
                    requireExitCodeZero: true,
                    cancellationToken: ExecutionContext.CancellationToken);
            }
        }

        private void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            // This does not need to be inside of a critical section.
            // The logging queues and command handlers are thread-safe.
            if (!CommandManager.TryProcessCommand(ExecutionContext, e.Data))
            {
                ExecutionContext.Output(e.Data);
            }
        }
    }
}
