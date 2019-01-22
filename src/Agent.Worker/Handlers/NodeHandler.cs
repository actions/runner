using Microsoft.VisualStudio.Services.Agent.Util;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
{
    [ServiceLocator(Default = typeof(NodeHandler))]
    public interface INodeHandler : IHandler
    {
        // Data can be of these two types: NodeHandlerData, and Node10HandlerData
        BaseNodeHandlerData Data { get; set; }
    }

    public sealed class NodeHandler : Handler, INodeHandler
    {
        private static Regex _vstsTaskLibVersionNeedsFix = new Regex("^[0-2]\\.[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static string[] _extensionsNode6 ={
            "if (process.versions.node && process.versions.node.match(/^5\\./)) {",
            "   String.prototype.startsWith = function (str) {",
            "       return this.slice(0, str.length) == str;",
            "   };",
            "   String.prototype.endsWith = function (str) {",
            "       return this.slice(-str.length) == str;",
            "   };",
            "};",
            "String.prototype.isEqual = function (ignoreCase, str) {",
            "   var str1 = this;",
            "   if (ignoreCase) {",
            "       str1 = str1.toLowerCase();",
            "       str = str.toLowerCase();",
            "       }",
            "   return str1 === str;",
            "};"
        };

        public BaseNodeHandlerData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.Directory(TaskDirectory, nameof(TaskDirectory));

#if !OS_WINDOWS
            // Ensure compat vso-task-lib exist at the root of _work folder
            // This will make vsts-agent works against 2015 RTM/QU1 TFS, since tasks in those version doesn't package with task lib
            // Put the 0.5.5 version vso-task-lib into the root of _work/node_modules folder, so tasks are able to find those lib.
            if (!File.Exists(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), "node_modules", "vso-task-lib", "package.json")))
            {
                string vsoTaskLibFromExternal = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "vso-task-lib");
                string compatVsoTaskLibInWork = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), "node_modules", "vso-task-lib");
                IOUtil.CopyDirectory(vsoTaskLibFromExternal, compatVsoTaskLibInWork, ExecutionContext.CancellationToken);
            }
#endif

            // Update the env dictionary.
            AddInputsToEnvironment();
            AddEndpointsToEnvironment();
            AddSecureFilesToEnvironment();
            AddVariablesToEnvironment();
            AddTaskVariablesToEnvironment();
            AddPrependPathToEnvironment();

            // Resolve the target script.
            string target = Data.Target;
            ArgUtil.NotNullOrEmpty(target, nameof(target));
            target = Path.Combine(TaskDirectory, target);
            ArgUtil.File(target, nameof(target));

            // Resolve the working directory.
            string workingDirectory = Data.WorkingDirectory;
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = ExecutionContext.Variables.Get(Constants.Variables.System.DefaultWorkingDirectory);
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    workingDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                }
            }

            // fix vsts-task-lib for node 6.x
            // vsts-task-lib 0.6/0.7/0.8/0.9/2.0-preview implemented String.prototype.startsWith and String.prototype.endsWith since Node 5.x doesn't have them.
            // however the implementation is added in node 6.x, the implementation in vsts-task-lib is different.
            // node 6.x's implementation takes 2 parameters str.endsWith(searchString[, length]) / str.startsWith(searchString[, length])
            // the implementation vsts-task-lib had only takes one parameter str.endsWith(searchString) / str.startsWith(searchString).
            // as long as vsts-task-lib be loaded into memory, it will overwrite the implementation node 6.x has, 
            // so any scirpt that use the second parameter (length) will encounter unexpected result.
            // to avoid customer hit this error, we will modify the file (extensions.js) under vsts-task-lib module folder when customer choose to use Node 6.x
            Trace.Info("Inspect node_modules folder, make sure vsts-task-lib doesn't overwrite String.startsWith/endsWith.");
            FixVstsTaskLibModule();

            StepHost.OutputDataReceived += OnDataReceived;
            StepHost.ErrorDataReceived += OnDataReceived;

            string file;
            if (!string.IsNullOrEmpty(ExecutionContext.Container?.ContainerBringNodePath))
            {
                file = ExecutionContext.Container.ContainerBringNodePath;
            }
            else
            {
                file = GetNodeLocation();
            }

            // Format the arguments passed to node.
            // 1) Wrap the script file path in double quotes.
            // 2) Escape double quotes within the script file path. Double-quote is a valid
            // file name character on Linux.
            string arguments = StepHost.ResolvePathForStepHost(StringUtil.Format(@"""{0}""", target.Replace(@"""", @"\""")));

#if OS_WINDOWS
            // It appears that node.exe outputs UTF8 when not in TTY mode.
            Encoding outputEncoding = Encoding.UTF8;
#else
            // Let .NET choose the default.
            Encoding outputEncoding = null;
#endif

            // Execute the process. Exit code 0 should always be returned.
            // A non-zero exit code indicates infrastructural failure.
            // Task failure should be communicated over STDOUT using ## commands.
            Task step = StepHost.ExecuteAsync(workingDirectory: StepHost.ResolvePathForStepHost(workingDirectory),
                                              fileName: StepHost.ResolvePathForStepHost(file),
                                              arguments: arguments,
                                              environment: Environment,
                                              requireExitCodeZero: true,
                                              outputEncoding: outputEncoding,
                                              killProcessOnCancel: false,
                                              inheritConsoleHandler: !ExecutionContext.Variables.Retain_Default_Encoding,
                                              cancellationToken: ExecutionContext.CancellationToken);

            // Wait for either the node exit or force finish through ##vso command
            await System.Threading.Tasks.Task.WhenAny(step, ExecutionContext.ForceCompleted);

            if (ExecutionContext.ForceCompleted.IsCompleted)
            {
                ExecutionContext.Debug("The task was marked as \"done\", but the process has not closed after 5 seconds. Treating the task as complete.");
            }
            else
            {
                await step;
            }
        }

        public string GetNodeLocation()
        {
            bool useNode10 = ExecutionContext.Variables.GetBoolean("AGENT_USE_NODE10")
                ?? StringUtil.ConvertToBoolean(System.Environment.GetEnvironmentVariable("AGENT_USE_NODE10"), false);
            bool taskHasNode10Data = Data is Node10HandlerData;
            string nodeFolder = (taskHasNode10Data || useNode10) ? "node10" : "node";

            Trace.Info($"Task.json has node10 handler data: {taskHasNode10Data}, use node10 for node tasks: {useNode10}");

            return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals),
                nodeFolder,
                "bin",
                $"node{IOUtil.ExeExtension}");
        }

        private void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            // drop any outputs after the task get force completed.
            if (ExecutionContext.ForceCompleted.IsCompleted)
            {
                return;
            }

            // This does not need to be inside of a critical section.
            // The logging queues and command handlers are thread-safe.
            if (!CommandManager.TryProcessCommand(ExecutionContext, e.Data))
            {
                ExecutionContext.Output(e.Data);
            }
        }

        private void FixVstsTaskLibModule()
        {
            // to avoid modify node_module all the time, we write a .node6 file to indicate we finsihed scan and modify.
            // the current task is good for node 6.x
            if (File.Exists(TaskDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".node6"))
            {
                Trace.Info("This task has already been scanned and corrected, no more operation needed.");
            }
            else
            {
                Trace.Info("Scan node_modules folder, looking for vsts-task-lib\\extensions.js");
                try
                {
                    foreach (var file in new DirectoryInfo(TaskDirectory).EnumerateFiles("extensions.js", SearchOption.AllDirectories))
                    {
                        if (string.Equals(file.Directory.Name, "vsts-task-lib", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(file.Directory.Name, "vso-task-lib", StringComparison.OrdinalIgnoreCase))
                        {
                            if (File.Exists(Path.Combine(file.DirectoryName, "package.json")))
                            {
                                // read package.json, we only do the fix for 0.x->2.x
                                JObject packageJson = JObject.Parse(File.ReadAllText(Path.Combine(file.DirectoryName, "package.json")));

                                JToken versionToken;
                                if (packageJson.TryGetValue("version", StringComparison.OrdinalIgnoreCase, out versionToken))
                                {
                                    if (_vstsTaskLibVersionNeedsFix.IsMatch(versionToken.ToString()))
                                    {
                                        Trace.Info($"Fix extensions.js file at '{file.FullName}'. The vsts-task-lib vsersion is '{versionToken.ToString()}'");

                                        // take backup of the original file
                                        File.Copy(file.FullName, Path.Combine(file.DirectoryName, "extensions.js.vstsnode5"));
                                        File.WriteAllLines(file.FullName, _extensionsNode6);
                                    }
                                }
                            }
                        }
                    }

                    File.WriteAllText(TaskDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".node6", string.Empty);
                    Trace.Info("Finished scan and correct extensions.js under vsts-task-lib");
                }
                catch (Exception ex)
                {
                    Trace.Error("Unable to scan and correct potential bug in extensions.js of vsts-task-lib.");
                    Trace.Error(ex);
                }
            }
        }
    }
}
