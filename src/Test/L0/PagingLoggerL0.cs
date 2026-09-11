using Moq;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class PagingLoggerL0
    {
        private const string LogData = "messagemessagemessagemessagemessagemessagemessagemessageXPLATmessagemessagemessagemessagemessagemessagemessagemessage";
        private const int PagesToWrite = 2;
        private Mock<IJobServerQueue> _jobServerQueue;

        public PagingLoggerL0()
        {
            _jobServerQueue = new Mock<IJobServerQueue>();
            PagingLogger.PagingFolder = "pages_" + Guid.NewGuid().ToString();
        }

        private void CleanLogFolder()
        {
            using (TestHostContext hc = new(this))
            {
                //clean test data if any old test forgot
                string pagesFolder = Path.Combine(hc.GetDirectory(WellKnownDirectory.Diag), PagingLogger.PagingFolder);
                if (Directory.Exists(pagesFolder))
                {
                    Directory.Delete(pagesFolder, true);
                }
            }
        }

        //WriteAndShipLog test will write "PagesToWrite" pages of data,
        //verify file content on the disk and check if API to ship data is invoked
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WriteAndShipLog()
        {
            CleanLogFolder();

            try
            {
                //Arrange
                using (var hc = new TestHostContext(this))
                {
                    var pagingLogger = new PagingLogger();
                    hc.SetSingleton<IJobServerQueue>(_jobServerQueue.Object);
                    pagingLogger.Initialize(hc);
                    Guid timeLineId = Guid.NewGuid();
                    Guid timeLineRecordId = Guid.NewGuid();
                    int totalBytes = PagesToWrite * PagingLogger.PageSize;
                    int bytesWritten = 0;
                    int logDataSize = System.Text.Encoding.UTF8.GetByteCount(LogData);
                    _jobServerQueue.Setup(x => x.QueueFileUpload(timeLineId, timeLineRecordId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true))
                        .Callback((Guid timelineId, Guid timelineRecordId, string type, string name, string path, bool deleteSource) =>
                        {
                            bool fileExists = File.Exists(path);
                            Assert.True(fileExists);

                            using (var freader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
                            {
                                string line;
                                while ((line = freader.ReadLine()) != null)
                                {
                                    Assert.EndsWith(LogData, line);
                                    bytesWritten += logDataSize;
                                }
                            }
                            File.Delete(path);
                        });

                    //Act
                    int bytesSent = 0;
                    pagingLogger.Setup(timeLineId, timeLineRecordId);
                    while (bytesSent < totalBytes)
                    {
                        pagingLogger.Write(LogData);
                        bytesSent += logDataSize;
                    }
                    pagingLogger.End();

                    //Assert
                    _jobServerQueue.Verify(x => x.QueueFileUpload(timeLineId, timeLineRecordId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true), Times.AtLeast(PagesToWrite));
                    Assert.Equal(bytesSent, bytesWritten);
                }
            }
            finally
            {
                //cleanup
                CleanLogFolder();
            }
        }

        //Try to ship empty log        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ShipEmptyLog()
        {
            CleanLogFolder();

            try
            {
                //Arrange
                using (var hc = new TestHostContext(this))
                {
                    var pagingLogger = new PagingLogger();
                    hc.SetSingleton<IJobServerQueue>(_jobServerQueue.Object);
                    pagingLogger.Initialize(hc);
                    Guid timeLineId = Guid.NewGuid();
                    Guid timeLineRecordId = Guid.NewGuid();
                    _jobServerQueue.Setup(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true));

                    //Act
                    pagingLogger.Setup(timeLineId, timeLineRecordId);
                    pagingLogger.End();

                    //Assert
                    _jobServerQueue.Verify(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true), Times.Exactly(0));
                }
            }
            finally
            {
                //cleanup
                CleanLogFolder();
            }
        }

        // Dispose without End() should flush/queue partial content.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Dispose_AfterPartialWrite_FlushesAndClosesFiles()
        {
            CleanLogFolder();

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var pagingLogger = new PagingLogger();
                    hc.SetSingleton<IJobServerQueue>(_jobServerQueue.Object);
                    pagingLogger.Initialize(hc);
                    Guid timeLineId = Guid.NewGuid();
                    Guid timeLineRecordId = Guid.NewGuid();

                    string queuedPagePath = null;
                    _jobServerQueue
                        .Setup(x => x.QueueFileUpload(timeLineId, timeLineRecordId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true))
                        .Callback((Guid _, Guid _, string _, string _, string path, bool _) => queuedPagePath = path);

                    string queuedBlockPath = null;
                    _jobServerQueue
                        .Setup(x => x.QueueResultsUpload(timeLineRecordId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<long>()))
                        .Callback((Guid _, string _, string path, string _, bool _, bool _, bool _, long _) => queuedBlockPath = path);

                    // Act: write once, then dispose without End().
                    pagingLogger.Setup(timeLineId, timeLineRecordId);
                    pagingLogger.Write(LogData);
                    pagingLogger.Dispose();

                    Assert.False(string.IsNullOrEmpty(queuedPagePath), "Dispose should have queued the partial page for upload.");
                    Assert.False(string.IsNullOrEmpty(queuedBlockPath), "Dispose should have queued the partial block for upload with finalize=true.");

                    // Verify flushed content reached queued files.
                    Assert.Contains(LogData, File.ReadAllText(queuedPagePath));
                    Assert.Contains(LogData, File.ReadAllText(queuedBlockPath));

                    // Cleanup files created outside the normal callback path.
                    File.Delete(queuedPagePath);
                    File.Delete(queuedBlockPath);
                }
            }
            finally
            {
                CleanLogFolder();
            }
        }

        // Dispose should be idempotent.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Dispose_IsIdempotent()
        {
            CleanLogFolder();

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var pagingLogger = new PagingLogger();
                    hc.SetSingleton<IJobServerQueue>(_jobServerQueue.Object);
                    pagingLogger.Initialize(hc);
                    Guid timeLineId = Guid.NewGuid();
                    Guid timeLineRecordId = Guid.NewGuid();

                    int queueFileUploadCount = 0;
                    int queueResultsUploadCount = 0;
                    _jobServerQueue
                        .Setup(x => x.QueueFileUpload(timeLineId, timeLineRecordId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true))
                        .Callback(() => queueFileUploadCount++);
                    _jobServerQueue
                        .Setup(x => x.QueueResultsUpload(timeLineRecordId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<long>()))
                        .Callback(() => queueResultsUploadCount++);

                    pagingLogger.Setup(timeLineId, timeLineRecordId);
                    pagingLogger.Write(LogData);
                    pagingLogger.Dispose();
                    pagingLogger.Dispose();

                    Assert.Equal(1, queueFileUploadCount);
                    Assert.Equal(1, queueResultsUploadCount);
                }
            }
            finally
            {
                CleanLogFolder();
            }
        }

        // Dispose after End() should be a no-op.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Dispose_AfterEnd_IsNoOp()
        {
            CleanLogFolder();

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var pagingLogger = new PagingLogger();
                    hc.SetSingleton<IJobServerQueue>(_jobServerQueue.Object);
                    pagingLogger.Initialize(hc);
                    Guid timeLineId = Guid.NewGuid();
                    Guid timeLineRecordId = Guid.NewGuid();

                    int queueCount = 0;
                    _jobServerQueue
                        .Setup(x => x.QueueFileUpload(timeLineId, timeLineRecordId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true))
                        .Callback(() => queueCount++);

                    pagingLogger.Setup(timeLineId, timeLineRecordId);
                    pagingLogger.Write(LogData);
                    pagingLogger.End();
                    int afterEnd = queueCount;

                    pagingLogger.Dispose();

                    Assert.Equal(afterEnd, queueCount);
                }
            }
            finally
            {
                CleanLogFolder();
            }
        }

        // Dispose before Write() should not queue uploads.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Dispose_BeforeAnyWrite_DoesNotThrow()
        {
            CleanLogFolder();

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var pagingLogger = new PagingLogger();
                    hc.SetSingleton<IJobServerQueue>(_jobServerQueue.Object);
                    pagingLogger.Initialize(hc);

                    int queueCount = 0;
                    _jobServerQueue
                        .Setup(x => x.QueueFileUpload(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                        .Callback(() => queueCount++);

                    pagingLogger.Setup(Guid.NewGuid(), Guid.NewGuid());
                    pagingLogger.Dispose();

                    Assert.Equal(0, queueCount);
                }
            }
            finally
            {
                CleanLogFolder();
            }
        }

        // Safety net: close orphaned _pageData when _pageWriter was never assigned.
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Dispose_ReleasesOrphanedFileStream_WhenWriterWasNeverAssigned()
        {
            CleanLogFolder();

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var pagingLogger = new PagingLogger();
                    hc.SetSingleton<IJobServerQueue>(_jobServerQueue.Object);
                    pagingLogger.Initialize(hc);
                    pagingLogger.Setup(Guid.NewGuid(), Guid.NewGuid());

                    // Force partial-init state: _pageData set, _pageWriter null.
                    var pagesFolder = Path.Combine(hc.GetDirectory(WellKnownDirectory.Diag), PagingLogger.PagingFolder);
                    Directory.CreateDirectory(pagesFolder);
                    var orphanPath = Path.Combine(pagesFolder, $"orphan-{Guid.NewGuid():N}.log");
                    var orphanStream = new FileStream(orphanPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

                    typeof(PagingLogger)
                        .GetField("_pageData", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(pagingLogger, orphanStream);

                    pagingLogger.Dispose();

                    var closedStream = (FileStream)typeof(PagingLogger)
                        .GetField("_pageData", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(pagingLogger);
                    Assert.Null(closedStream);

                    Assert.True(orphanStream.SafeFileHandle.IsClosed, "Dispose should have closed the orphaned FileStream's handle.");
                    File.Delete(orphanPath);
                }
            }
            finally
            {
                CleanLogFolder();
            }
        }

    }
}
