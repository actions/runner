using System;
using System.IO;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(PagingLogger))]
    public interface IPagingLogger : IRunnerService
    {
        long TotalLines { get; }
        void Setup(Guid timelineId, Guid timelineRecordId);

        void Write(string message);

        void End();
    }

    public class PagingLogger : RunnerService, IPagingLogger
    {
        public static string PagingFolder = "pages";

        // 8 MB
        public const int PageSize = 8 * 1024 * 1024;

        private Guid _timelineId;
        private Guid _timelineRecordId;
        private FileStream _pageData;
        private StreamWriter _pageWriter;
        private int _byteCount;
        private int _pageCount;
        private long _totalLines;
        private string _dataFileName;
        private string _pagesFolder;
        private IJobServerQueue _jobServerQueue;

        // For Results
        public static string BlocksFolder = "blocks";

        // 2 MB
        public const int BlockSize = 2 * 1024 * 1024;

        private string _resultsDataFileName;
        private FileStream _resultsBlockData;
        private StreamWriter _resultsBlockWriter;
        private string _resultsBlockFolder;
        private int _blockByteCount;
        private int _blockCount;

        public long TotalLines => _totalLines;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _totalLines = 0;
            _pagesFolder = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Diag), PagingFolder);
            Directory.CreateDirectory(_pagesFolder);
            _resultsBlockFolder = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Diag), BlocksFolder);
            Directory.CreateDirectory(_resultsBlockFolder);
            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
        }

        public void Setup(Guid timelineId, Guid timelineRecordId)
        {
            _timelineId = timelineId;
            _timelineRecordId = timelineRecordId;
        }

        //
        // Write a metadata file with id etc, point to pages on disk.
        // Each page is a guid_#.  As a page rolls over, it events it's done
        // and the consumer queues it for upload
        // Ensure this is lazy.  Create a page on first write
        //
        public void Write(string message)
        {
            // lazy creation on write
            if (_pageWriter == null)
            {
                NewPage();
            }

            if (_resultsBlockWriter == null)
            {
                NewBlock();
            }

            string line = $"{DateTime.UtcNow.ToString("O")} {message}";
            _pageWriter.WriteLine(line);
            _resultsBlockWriter.WriteLine(line);

            _totalLines++;
            if (line.IndexOf('\n') != -1)
            {
                foreach (char c in line)
                {
                    if (c == '\n')
                    {
                        _totalLines++;
                    }
                }
            }

            var bytes = System.Text.Encoding.UTF8.GetByteCount(line); 
            _byteCount += bytes; 
            _blockByteCount += bytes;
            if (_byteCount >= PageSize)
            {
                NewPage();
            }

            if (_blockByteCount >= BlockSize)
            {
                NewBlock();
            }

        }

        public void End()
        {
            EndPage();
            EndBlock(true);
        }

        private void NewPage()
        {
            EndPage();
            _byteCount = 0;
            _dataFileName = Path.Combine(_pagesFolder, $"{_timelineId}_{_timelineRecordId}_{++_pageCount}.log");
            _pageData = new FileStream(_dataFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            _pageWriter = new StreamWriter(_pageData, System.Text.Encoding.UTF8);
        }

        private void EndPage()
        {
            if (_pageWriter != null)
            {
                _pageWriter.Flush();
                _pageData.Flush();
                //The StreamWriter object calls Dispose() on the provided Stream object when StreamWriter.Dispose is called.
                _pageWriter.Dispose();
                _pageWriter = null;
                _pageData = null;
                _jobServerQueue.QueueFileUpload(_timelineId, _timelineRecordId, "DistributedTask.Core.Log", "CustomToolLog", _dataFileName, true);
            }
        }

        private void NewBlock()
        {
            EndBlock(false);
            _blockByteCount = 0;
            _resultsDataFileName = Path.Combine(_resultsBlockFolder, $"{_timelineId}_{_timelineRecordId}_{++_blockCount}.block");
            _resultsBlockData = new FileStream(_resultsDataFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            _resultsBlockWriter = new StreamWriter(_resultsBlockData, System.Text.Encoding.UTF8);
        }

        private void EndBlock(bool finalize)
        {
            if (_resultsBlockWriter != null)
            {
                _resultsBlockWriter.Flush();
                _resultsBlockData.Flush();
                _resultsBlockWriter.Dispose();
                _resultsBlockWriter = null;
                _resultsBlockData = null;
                _jobServerQueue.QueueResultsUpload(_timelineRecordId, "ResultsLog", _resultsDataFileName, "Results.Core.Log", true, finalize);
            }
        }
    }
}
