using GitHub.Runner.Common.Util;
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

        public long TotalLines => _totalLines;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _totalLines = 0;
            _pagesFolder = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Diag), PagingFolder);
            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
            Directory.CreateDirectory(_pagesFolder);
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
                Create();
            }

            string line = $"{DateTime.UtcNow.ToString("O")} {message}";
            _pageWriter.WriteLine(line);

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

            _byteCount += System.Text.Encoding.UTF8.GetByteCount(line);
            if (_byteCount >= PageSize)
            {
                NewPage();
            }
        }

        public void End()
        {
            EndPage();
        }

        private void Create()
        {
            NewPage();
        }

        private void NewPage()
        {
            EndPage();
            _byteCount = 0;
            _dataFileName = Path.Combine(_pagesFolder, $"{_timelineId}_{_timelineRecordId}_{++_pageCount}.log");
            _pageData = new FileStream(_dataFileName, FileMode.CreateNew);
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
    }
}
