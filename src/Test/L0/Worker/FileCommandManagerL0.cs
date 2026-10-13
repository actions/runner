using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class FileCommandManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitializeFiles_InvokesPopulateInitialContents_OncePerExtension()
        {
            using (var hostContext = Setup(out var executionContext, out var ext))
            {
                var manager = new FileCommandManager();
                manager.Initialize(hostContext);

                manager.InitializeFiles(executionContext, null);

                Assert.Equal(1, ext.PopulateCallCount);
                Assert.True(File.Exists(ext.LastPopulatedPath));

                // A second invocation should populate again with the new
                // per-step file (file path rotates between calls).
                var firstPath = ext.LastPopulatedPath;
                manager.InitializeFiles(executionContext, null);
                Assert.Equal(2, ext.PopulateCallCount);
                Assert.NotEqual(firstPath, ext.LastPopulatedPath);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitializeFiles_PopulateException_DoesNotAbortInitialization()
        {
            using (var hostContext = Setup(out var executionContext, out var ext))
            {
                ext.ThrowOnPopulate = true;

                var manager = new FileCommandManager();
                manager.Initialize(hostContext);

                // Must not throw — failures during populate should be
                // swallowed so a misbehaving extension cannot block step
                // setup.
                manager.InitializeFiles(executionContext, null);

                Assert.Equal(1, ext.PopulateCallCount);
            }
        }

        private TestHostContext Setup(out IExecutionContext executionContext, out RecordingFileCommand recordingExtension, [CallerMemberName] string name = "")
        {
            var hostContext = new TestHostContext(this, name);

            recordingExtension = new RecordingFileCommand();
            recordingExtension.Initialize(hostContext);

            var extensionManager = new Mock<IExtensionManager>();
            extensionManager.Setup(x => x.GetExtensions<IFileCommandExtension>())
                .Returns(new List<IFileCommandExtension> { recordingExtension });
            hostContext.SetSingleton<IExtensionManager>(extensionManager.Object);

            var ec = new Mock<IExecutionContext>();
            ec.Setup(x => x.SetGitHubContext(It.IsAny<string>(), It.IsAny<string>()));
            executionContext = ec.Object;

            return hostContext;
        }

        private sealed class RecordingFileCommand : RunnerService, IFileCommandExtension
        {
            public string ContextName => "recording";
            public string FilePrefix => "recording_";
            public Type ExtensionType => typeof(IFileCommandExtension);

            public int PopulateCallCount { get; private set; }
            public string LastPopulatedPath { get; private set; }
            public bool ThrowOnPopulate { get; set; }

            public void PopulateInitialContents(IExecutionContext context, string filePath, ContainerInfo container)
            {
                PopulateCallCount++;
                LastPopulatedPath = filePath;
                if (ThrowOnPopulate)
                {
                    throw new InvalidOperationException("intentional");
                }
            }

            public void ProcessCommand(IExecutionContext context, string filePath, ContainerInfo container)
            {
            }
        }
    }
}
