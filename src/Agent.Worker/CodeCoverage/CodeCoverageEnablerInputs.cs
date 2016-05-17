using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using static Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageCommandExtension;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerInputs
    {
        public CodeCoverageEnablerInputs(IExecutionContext context, string buildTool, Dictionary<string, string> eventProperties)
        {
            string classFilter;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.ClassFilter, out classFilter);

            string buildFile;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.BuildFile, out buildFile);

            string classFilesDirectories;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.ClassFilesDirectories, out classFilesDirectories);

            string sourceDirectories;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.SourceDirectories, out sourceDirectories);

            string summaryFile;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.SummaryFile, out summaryFile);

            string cCReportTask;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.CCReportTask, out cCReportTask);

            string reportBuildFile;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.ReportBuildFile, out reportBuildFile);

            string isMultiModuleInput;
            var isMultiModule = false;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.IsMultiModule, out isMultiModuleInput);
            if (!bool.TryParse(isMultiModuleInput, out isMultiModule) && buildTool.Equals("gradle", StringComparison.OrdinalIgnoreCase))
            {
                context.Output(StringUtil.Loc("IsMultiModuleParameterNotAvailable"));
            }

            string reportDirectory;
            eventProperties.TryGetValue(EnableCodeCoverageEventProperties.ReportDirectory, out reportDirectory);

            string include, exclude;
            CodeCoverageUtilities.GetFilters(classFilter, out include, out exclude);

            BuildFile = CodeCoverageUtilities.TrimNonEmptyParam(buildFile, "BuildFile");

            //validatebuild file exists
            if (!File.Exists(BuildFile))
            {
                throw new FileNotFoundException(StringUtil.Loc("FileDoesNotExist", BuildFile));
            }

            ClassFilesDirectories = classFilesDirectories;
            Include = include;
            Exclude = exclude;
            SourceDirectories = sourceDirectories;
            SummaryFile = summaryFile;
            ReportDirectory = reportDirectory;
            CCReportTask = cCReportTask;
            ReportBuildFile = reportBuildFile;
            IsMultiModule = isMultiModule;
        }

        public void VerifyInputsForJacocoAnt(IExecutionContext context)
        {
            ClassFilesDirectories = CodeCoverageUtilities.TrimNonEmptyParam(ClassFilesDirectories, "ClassFilesDirectories");
            SummaryFile = CodeCoverageUtilities.TrimNonEmptyParam(SummaryFile, "SummaryFile");
            ReportDirectory = CodeCoverageUtilities.TrimNonEmptyParam(ReportDirectory, "ReportDirectory");
            CCReportTask = CodeCoverageUtilities.TrimNonEmptyParam(CCReportTask, "CodeCoverageReportTarget");
            ReportBuildFile = CodeCoverageUtilities.TrimNonEmptyParam(ReportBuildFile, "CodeCoverageReportBuildFile");
            CodeCoverageUtilities.ThrowIfClassFilesDirectoriesIsInvalid(ClassFilesDirectories);

            SourceDirectories = CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(context, SourceDirectories, StringUtil.Loc("SourceDirectoriesNotSpecified"));
        }

        public void VerifyInputsForCoberturaAnt(IExecutionContext context)
        {
            ClassFilesDirectories = CodeCoverageUtilities.TrimNonEmptyParam(ClassFilesDirectories, "ClassFilesDirectories");
            ReportDirectory = CodeCoverageUtilities.TrimNonEmptyParam(ReportDirectory, "ReportDirectory");
            CCReportTask = CodeCoverageUtilities.TrimNonEmptyParam(CCReportTask, "CodeCoverageReportTarget");
            ReportBuildFile = CodeCoverageUtilities.TrimNonEmptyParam(ReportBuildFile, "CodeCoverageReportBuildFile");
            CodeCoverageUtilities.ThrowIfClassFilesDirectoriesIsInvalid(ClassFilesDirectories);

            SourceDirectories = CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(context, SourceDirectories, StringUtil.Loc("SourceDirectoriesNotSpecified"));
        }

        public void VerifyInputsForJacocoMaven()
        {
            SummaryFile = CodeCoverageUtilities.TrimNonEmptyParam(SummaryFile, "SummaryFile");
            ReportDirectory = CodeCoverageUtilities.TrimNonEmptyParam(ReportDirectory, "ReportDirectory");
            CodeCoverageUtilities.ThrowIfClassFilesDirectoriesIsInvalid(ClassFilesDirectories);
        }

        public void VerifyInputsForJacocoGradle()
        {
            ClassFilesDirectories = CodeCoverageUtilities.TrimNonEmptyParam(ClassFilesDirectories, "ClassFilesDirectory");
            SummaryFile = CodeCoverageUtilities.TrimNonEmptyParam(SummaryFile, "SummaryFile");
            ReportDirectory = CodeCoverageUtilities.TrimNonEmptyParam(ReportDirectory, "ReportDirectory");
        }

        public void VerifyInputsForCoberturaGradle()
        {
            ClassFilesDirectories = CodeCoverageUtilities.TrimNonEmptyParam(ClassFilesDirectories, "ClassFilesDirectory");
            ReportDirectory = CodeCoverageUtilities.TrimNonEmptyParam(ReportDirectory, "ReportDirectory");
        }

        public string BuildFile { get; private set; }

        public string ClassFilesDirectories { get; private set; }

        public string Include { get; private set; }

        public string Exclude { get; private set; }

        public string SourceDirectories { get; private set; }

        public string SummaryFile { get; private set; }

        public string ReportDirectory { get; private set; }

        public string CCReportTask { get; private set; }

        public string ReportBuildFile { get; private set; }

        public bool IsMultiModule { get; private set; }
    }
}