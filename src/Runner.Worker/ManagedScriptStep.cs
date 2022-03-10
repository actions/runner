// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System.Linq;

namespace GitHub.Runner.Worker
{
    // This is for internal testing and is not publicly supported. This will be removed from the agent at a later time.
    public class ManagedScriptStep : RunnerService, IStep
    {
        public ManagedScriptStep(
            string scriptPath,
            string condition,
            string displayName,
            ActionRunStage stage)
        {
            ScriptPath = scriptPath;
            Condition = condition;
            DisplayName = displayName;
            Stage = stage;
        }

        public string ScriptPath { get; private set; }
        public string Condition { get; set; }
        public string DisplayName { get; set; }
        public ActionRunStage Stage { get; }

        public bool Enabled => true;
        public string AccessToken { get; set; }


        public TemplateToken ContinueOnError => new BooleanToken(null, null, null, false);
        public TemplateToken Timeout => new NumberToken(null, null, null, 0);

        public IExecutionContext ExecutionContext { get; set; }

        public async Task RunAsync()
        {
            // Validate script file.
            if (!File.Exists(ScriptPath))
            {
                throw new IOException("File doesn't exist");
            }

            this.WriteWebhookPayload(HostContext, Trace);

            // Create the handler data.
            var scriptDirectory = Path.GetDirectoryName(ScriptPath);

            // Create the handler invoker
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            // Create the handler
            var handlerFactory = HostContext.GetService<IHandlerFactory>();
            
            var prependPath = string.Join(Path.PathSeparator.ToString(), ExecutionContext.Global.PrependPath.Reverse<string>());
            ScriptHandlerHelpers.WhichShell(ScriptPath, Trace, prependPath);

            Dictionary<string, string> inputs = new()
            {
                ["path"] = ScriptPath,
                ["shell"] = "/usr/bin/bash -e {0}"
            };
            var handler = handlerFactory.Create(
                            ExecutionContext,
                            null,
                            stepHost,
                            new ScriptActionExecutionData(),
                            inputs, // TODO: set shell. ps1,sh
                            new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                            ExecutionContext.Global.Variables,
                            actionDirectory: scriptDirectory,
                            localActionContainerSetupSteps: null);

            handler.PrepareExecution(Stage);

            // Setup File Command Manager
            var fileCommandManager = HostContext.CreateService<IFileCommandManager>();
            fileCommandManager.InitializeFiles(ExecutionContext, null);
            try
            {
                await handler.RunAsync(Stage);
            }
            finally
            {
                fileCommandManager.ProcessFiles(ExecutionContext, ExecutionContext.Global.Container);
            }
        }
    }
}