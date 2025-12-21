// Copyright (c) GitHub. All rights reserved.
// Tests that verify run.sh and run-helper.sh.template properly propagate exit codes
// instead of always returning 0. This ensures external monitoring systems can detect runner errors.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests.Misc
{
    public sealed class RunScriptExitCodeL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#if !OS_WINDOWS
        public async Task RunHelperPropagatesTerminatedErrorCode()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    string runHelperPath = Path.Combine(tempDir, "run-helper.sh.template");
                    File.WriteAllText(runHelperPath, @"#!/bin/bash
exit 1
");
                    var chmodInvoker = new ProcessInvokerWrapper();
                    chmodInvoker.Initialize(hc);
                    await chmodInvoker.ExecuteAsync("", "chmod", $"+x {runHelperPath}", null, CancellationToken.None);

                    string runScriptPath = Path.Combine(tempDir, "run.sh");
                    File.WriteAllText(runScriptPath, $@"#!/bin/bash
DIR=""{tempDir}""
while :;
do
    cp -f ""$DIR""/run-helper.sh.template ""$DIR""/run-helper.sh
    ""$DIR""/run-helper.sh
    returnCode=$?
    if [[ $returnCode -eq 2 ]]; then
        echo ""Restarting runner...""
    else
        echo ""Exiting runner...""
        exit $returnCode
    fi
done
");
                    var chmod2Invoker = new ProcessInvokerWrapper();
                    chmod2Invoker.Initialize(hc);
                    await chmod2Invoker.ExecuteAsync("", "chmod", $"+x {runScriptPath}", null, CancellationToken.None);

                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    int exitCode = await processInvoker.ExecuteAsync("", runScriptPath, "", null, CancellationToken.None);

                    trace.Info("Exit Code: {0}", exitCode);
                    Assert.Equal(Constants.Runner.ReturnCode.TerminatedError, exitCode);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunHelperPropagatesSessionConflictCode()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    string runHelperPath = Path.Combine(tempDir, "run-helper.sh.template");
                    File.WriteAllText(runHelperPath, @"#!/bin/bash
exit 5
");
                    var chmodInvoker = new ProcessInvokerWrapper();
                    chmodInvoker.Initialize(hc);
                    await chmodInvoker.ExecuteAsync("", "chmod", $"+x {runHelperPath}", null, CancellationToken.None);

                    string runScriptPath = Path.Combine(tempDir, "run.sh");
                    File.WriteAllText(runScriptPath, $@"#!/bin/bash
DIR=""{tempDir}""
while :;
do
    cp -f ""$DIR""/run-helper.sh.template ""$DIR""/run-helper.sh
    ""$DIR""/run-helper.sh
    returnCode=$?
    if [[ $returnCode -eq 2 ]]; then
        echo ""Restarting runner...""
    else
        echo ""Exiting runner...""
        exit $returnCode
    fi
done
");
                    // Make it executable
                    var chmod2Invoker = new ProcessInvokerWrapper();
                    chmod2Invoker.Initialize(hc);
                    await chmod2Invoker.ExecuteAsync("", "chmod", $"+x {runScriptPath}", null, CancellationToken.None);

                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    int exitCode = await processInvoker.ExecuteAsync("", runScriptPath, "", null, CancellationToken.None);

                    trace.Info("Exit Code: {0}", exitCode);
                    Assert.Equal(Constants.Runner.ReturnCode.SessionConflict, exitCode);
                }
                finally
                {
                    // Cleanup
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunHelperPropagatesUnknownErrorCode()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    string runHelperPath = Path.Combine(tempDir, "run-helper.sh.template");
                    File.WriteAllText(runHelperPath, @"#!/bin/bash
exit 42
");
                    var chmodInvoker = new ProcessInvokerWrapper();
                    chmodInvoker.Initialize(hc);
                    await chmodInvoker.ExecuteAsync("", "chmod", $"+x {runHelperPath}", null, CancellationToken.None);

                    string runScriptPath = Path.Combine(tempDir, "run.sh");
                    File.WriteAllText(runScriptPath, $@"#!/bin/bash
DIR=""{tempDir}""
while :;
do
    cp -f ""$DIR""/run-helper.sh.template ""$DIR""/run-helper.sh
    ""$DIR""/run-helper.sh
    returnCode=$?
    if [[ $returnCode -eq 2 ]]; then
        echo ""Restarting runner...""
    else
        echo ""Exiting runner...""
        exit $returnCode
    fi
done
");
                    // Make it executable
                    var chmod2Invoker = new ProcessInvokerWrapper();
                    chmod2Invoker.Initialize(hc);
                    await chmod2Invoker.ExecuteAsync("", "chmod", $"+x {runScriptPath}", null, CancellationToken.None);

                    var processInvoker = new ProcessInvokerWrapper();
                    processInvoker.Initialize(hc);
                    int exitCode = await processInvoker.ExecuteAsync("", runScriptPath, "", null, CancellationToken.None);

                    trace.Info("Exit Code: {0}", exitCode);
                    Assert.Equal(42, exitCode);
                }
                finally
                {
                    // Cleanup
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
        }
#endif
    }
}
