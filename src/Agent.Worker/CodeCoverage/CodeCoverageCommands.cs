using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public class CodeCoverageCommands : AgentService, ICommandExtension
    {
        private IExecutionContext _executionContext;
        private int _buildId;
        // publish code coverage inputs
        private string _codeCoverageTool;
        private string _summaryFileLocation;
        private string _reportDirectory;
        private List<string> _additionalCodeCoverageFiles;

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            if (string.Equals(command.Event, WellKnownResultsCommand.PublishCodeCoverage, StringComparison.OrdinalIgnoreCase))
            {
                ArgUtil.NotNull(context, nameof(context));
                _executionContext = context;
                LoadPublishCodeCoverageInputs(command.Properties);
                PublishCodeCoverage();
            }
            else
            {
                throw new Exception(StringUtil.Loc("CodeCoverageCommandNotFound", command.Event));
            }
        }

        public Type ExtensionType
        {
            get
            {
                return typeof(ICommandExtension);
            }
        }

        public string CommandArea
        {
            get
            {
                return "codecoverage";
            }
        }

        #region publish code coverage helper methods
        private void PublishCodeCoverage()
        {
            Client.VssConnection connection = WorkerUtilies.GetVssConnection(_executionContext);

            _buildId = _executionContext.Variables.Build_BuildId ?? -1;
            if (_buildId < 0)
            {
                //In case the publishing codecoverage is not applicable for current Host type we continue without publishing
                _executionContext.Warning(StringUtil.Loc("CodeCoveragePublishIsValidOnlyForBuild"));
                return;
            }
            _executionContext.Debug(StringUtil.Format("Fetched BuildId '{0}'.", _buildId));

            string project = _executionContext.Variables.System_TeamProject;
            string collectionUrl = _executionContext.Variables.System_TFCollectionUrl;

            var codeCoveragePublisher = HostContext.GetService<ICodeCoveragePublisher>();
            codeCoveragePublisher.InitializePublisher(_executionContext, _buildId, project, collectionUrl, connection);

            GenerateAndPublishCodeCoverageSummary(codeCoveragePublisher);
            PublishCodeCoverageAttachments(codeCoveragePublisher);
        }

        private void GenerateAndPublishCodeCoverageSummary(ICodeCoveragePublisher codeCoveragePublisher)
        {
            var reader = GetCodeCoverageSummaryReader(_codeCoverageTool);
            var coverageData = reader.GetCodeCoverageSummary(_executionContext, _summaryFileLocation);

            if (coverageData == null)
            {
                _executionContext.Warning(StringUtil.Loc("CodeCoverageDataIsNull"));
            }

            _executionContext.Output(StringUtil.Loc("PublishingCodeCoverage"));
            codeCoveragePublisher.PublishCodeCoverageSummary(coverageData, _executionContext.CancellationToken);
        }

        private void PublishCodeCoverageAttachments(ICodeCoveragePublisher codeCoveragePublisher)
        {
            var filesToPublish = new List<Tuple<string, string>>();
            string additionalCodeCoverageFilePath = null;
            var newReportDirectory = _reportDirectory;

            if (!Directory.Exists(newReportDirectory))
            {
                newReportDirectory = GetCoverageDirectory(_buildId.ToString(), CodeCoverageUtilities.ReportDirectory);
                Directory.CreateDirectory(newReportDirectory);
            }

            var summaryFileName = Path.GetFileName(_summaryFileLocation);
            var destinationSummaryFile = Path.Combine(newReportDirectory, CodeCoverageUtilities.SummaryFileDirectory + _buildId, summaryFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationSummaryFile));
            File.Copy(_summaryFileLocation, destinationSummaryFile, true);


            filesToPublish.Add(new Tuple<string, string>(newReportDirectory, GetCoverageDirectoryName(_buildId.ToString(), CodeCoverageUtilities.ReportDirectory)));

            if (_additionalCodeCoverageFiles != null && _additionalCodeCoverageFiles.Count != 0)
            {
                additionalCodeCoverageFilePath = GetCoverageDirectory(_buildId.ToString(), CodeCoverageUtilities.RawFilesDirectory);
                CodeCoverageUtilities.CopyFilesFromFileListWithDirStructure(_additionalCodeCoverageFiles, ref additionalCodeCoverageFilePath);
                filesToPublish.Add(new Tuple<string, string>(additionalCodeCoverageFilePath, GetCoverageDirectoryName(_buildId.ToString(), CodeCoverageUtilities.RawFilesDirectory)));
            }

            if (filesToPublish.Count > 0)
            {
                _executionContext.Output(StringUtil.Loc("PublishingCodeCoverageFiles"));
                codeCoveragePublisher.PublishCodeCoverageFiles(filesToPublish, _executionContext.CancellationToken, File.Exists(Path.Combine(newReportDirectory, CodeCoverageUtilities.DefaultIndexFile)));
            }

            if (!string.IsNullOrEmpty(additionalCodeCoverageFilePath))
            {
                if (Directory.Exists(additionalCodeCoverageFilePath))
                {
                    Directory.Delete(path: additionalCodeCoverageFilePath, recursive: true);
                }
            }

            var summaryFileDirectory = Path.GetDirectoryName(destinationSummaryFile);
            if (Directory.Exists(summaryFileDirectory))
            {
                Directory.Delete(path: summaryFileDirectory, recursive: true);
            }

            if (!Directory.Exists(_reportDirectory))
            {
                //delete the generated report directory
                Directory.Delete(path: newReportDirectory, recursive: true);
            }
        }

        private ICodeCoverageSummaryReader GetCodeCoverageSummaryReader(string codeCoverageTool)
        {
            var extensionManager = HostContext.GetService<IExtensionManager>();
            ICodeCoverageSummaryReader summaryReader = (extensionManager.GetExtensions<ICodeCoverageSummaryReader>()).FirstOrDefault(x => codeCoverageTool.Equals(x.Name, StringComparison.OrdinalIgnoreCase));

            if (summaryReader == null)
            {
                throw new ArgumentException(StringUtil.Loc("UnknownCodeCoverageTool", codeCoverageTool));
            }
            return summaryReader;
        }

        private void LoadPublishCodeCoverageInputs(Dictionary<string, string> eventProperties)
        {
            //validate codecoverage tool input
            eventProperties.TryGetValue(PublishCodeCoverageEventProperties.CodeCoverageTool, out _codeCoverageTool);
            if (string.IsNullOrEmpty(_codeCoverageTool))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "CodeCoverageTool"));
            }

            //validate summary file input
            eventProperties.TryGetValue(PublishCodeCoverageEventProperties.SummaryFile, out _summaryFileLocation);
            if (string.IsNullOrEmpty(_summaryFileLocation))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "SummaryFile"));
            }

            eventProperties.TryGetValue(PublishCodeCoverageEventProperties.ReportDirectory, out _reportDirectory);

            string additionalFilesInput;
            eventProperties.TryGetValue(PublishCodeCoverageEventProperties.AdditionalCodeCoverageFiles, out additionalFilesInput);
            if (!string.IsNullOrEmpty(additionalFilesInput) && additionalFilesInput.Split(',').Count() > 0)
            {
                _additionalCodeCoverageFiles = additionalFilesInput.Split(',').ToList<string>();
            }
        }

        private string GetCoverageDirectory(string buildId, string directoryName)
        {
            return Path.Combine(Path.GetTempPath(), GetCoverageDirectoryName(buildId, directoryName));
        }

        private string GetCoverageDirectoryName(string buildId, string directoryName)
        {
            return directoryName + "_" + buildId;
        }
        #endregion

        internal static class WellKnownResultsCommand
        {
            internal static readonly string PublishCodeCoverage = "publish";
        }

        internal static class PublishCodeCoverageEventProperties
        {
            internal static readonly string CodeCoverageTool = "codecoveragetool";
            internal static readonly string SummaryFile = "summaryfile";
            internal static readonly string ReportDirectory = "reportdirectory";
            internal static readonly string AdditionalCodeCoverageFiles = "additionalcodecoveragefiles";
        }
    }
}