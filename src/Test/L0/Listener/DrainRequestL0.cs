using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GitHub.Runner.Listener;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    // Signal handlers are process-wide; do not overlap other drain tests.
    [Collection("Ephemeral drain")]
    public sealed class DrainRequestL0
    {
        [Fact]
        public void RequestIsLatchedAndIdempotent()
        {
            using var drain = new DrainRequest(null, false);
            Assert.False(drain.IsRequested);
            drain.Request();
            drain.Request();
            Assert.True(drain.IsRequested);
        }

        [Fact]
        public void FileIsOptionalAndRequestSurvivesRemoval()
        {
            using var hc = new TestHostContext(this);
            string path = Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "custom-drain");
            Assert.False(File.Exists(path));
            using var disabled = new DrainRequest(null, false);
            using var drain = new DrainRequest(path, false);
            try
            {
                Assert.False(drain.IsRequested);
                File.WriteAllText(path, "");
                Assert.False(disabled.IsRequested);
                Assert.True(drain.IsRequested);
                File.Delete(path);
                Assert.True(drain.IsRequested);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Sigusr1RequestsDrainWithoutAFile()
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            {
                Assert.Throws<PlatformNotSupportedException>(() => new DrainRequest(null, true));
                return;
            }

            using var drain = new DrainRequest(null, true);
            Assert.False(drain.IsRequested);
            // Use the OS's named signal so the test independently checks our mapping.
            using var sender = Process.Start(new ProcessStartInfo("/bin/kill")
            {
                UseShellExecute = false,
                ArgumentList = { "-USR1", Environment.ProcessId.ToString() }
            });
            Assert.True(sender.WaitForExit(5000));
            Assert.Equal(0, sender.ExitCode);
            Assert.True(SpinWait.SpinUntil(() => drain.IsRequested, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void RunAcceptsBothDrainOptions()
        {
            using var hc = new TestHostContext(this);
            hc.SetSingleton<IPromptManager>(new Moq.Mock<IPromptManager>().Object);
            var command = new CommandSettings(hc, new[] { "run", "--drain-on-sigusr1", "--drain-file", "/run/runner/drain" });
            Assert.Empty(command.Validate());
            Assert.True(command.DrainOnSigusr1);
            Assert.Equal("/run/runner/drain", command.GetDrainFile());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MissingDrainFileValueIsRejected(bool emptyArgument)
        {
            using var hc = new TestHostContext(this);
            hc.SetSingleton<IPromptManager>(new Moq.Mock<IPromptManager>().Object);
            var args = emptyArgument ? new[] { "run", "--drain-file", "" } : new[] { "run", "--drain-file" };
            var command = new CommandSettings(hc, args);
            Assert.Throws<ArgumentException>(() => command.GetDrainFile());
        }
    }
}
