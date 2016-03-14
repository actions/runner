using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(PagingLogger))]
    public interface IPagingLogger : IAgentService
    {
        void Setup(Guid timelineId, Guid timelineRecordId);

        void Write(string message);        

        void End();
    }

    public class PagingLogger : AgentService, IPagingLogger
    {
        public const int PageSize = 256;

        private Guid _timelineId;
        private Guid _timelineRecordId;
        private string _pageId;
        private FileStream _pageData;
        private StreamWriter _pageWriter;
        private int _lineCount;
        private int _pageCount;
        private string _dataFileName;
        private string _pagesFolder;
        private IJobServerQueue _jobServerQueue;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _pageWriter = null;
            _pageData = null;
            _lineCount = 0;
            _pageCount = 0;
            _pageId = Guid.NewGuid().ToString();
            _pagesFolder = Path.Combine(IOUtil.GetDiagPath(), "pages");
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
            ArgUtil.NotEmpty(_timelineId, nameof(_timelineId));

            // lazy creation on write
            if (_pageWriter == null)
            {
                Create();
            }

            _pageWriter.WriteLine($"{DateTime.UtcNow.ToString("O")} {message}");

            // TODO: split lines - line count not completely accurate
            if (++_lineCount >= PageSize)
            {
                NewPage();
            }
        }

        private void Create()
        {
            NewPage();
        }

        private void NewPage()
        {
            EndPage();
            _lineCount = 0;
            _dataFileName = Path.Combine(_pagesFolder, $"{_pageId}_{++_pageCount}.page");
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

        public void End()
        {
            EndPage();
        }
    }
}