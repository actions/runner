using GitHub.Runner.Common.Util;
using Moq;
using System;
using System.IO;
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
            using (TestHostContext hc = new TestHostContext(this))
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
    }
}
