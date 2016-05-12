using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerInputs
    {
        public CodeCoverageEnablerInputs(string buildFile, string classFilesDirectories, string include, string exclude, string sourceDirectories,
                string summaryFile, string reportDirectory, string cCReportTask, string reportBuildFile, bool isMultiModule)
        {
            BuildFile = CodeCoverageUtilities.ThrowIfParameterEmpty(buildFile, "BuildFile");

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
            ClassFilesDirectories = CodeCoverageUtilities.ThrowIfParameterEmpty(ClassFilesDirectories, "ClassFilesDirectories");
            SummaryFile = CodeCoverageUtilities.ThrowIfParameterEmpty(SummaryFile, "SummaryFile");
            ReportDirectory = CodeCoverageUtilities.ThrowIfParameterEmpty(ReportDirectory, "ReportDirectory");
            CCReportTask = CodeCoverageUtilities.ThrowIfParameterEmpty(CCReportTask, "CodeCoverageReportTarget");
            ReportBuildFile = CodeCoverageUtilities.ThrowIfParameterEmpty(ReportBuildFile, "CodeCoverageReportBuildFile");

            CodeCoverageUtilities.ThrowIfClassFilesDirectoriesIsInvalid(ClassFilesDirectories);

            SourceDirectories = CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(context, SourceDirectories, StringUtil.Loc("SourceDirectoriesNotSpecified"));
        }

        public void VerifyInputsForCoberturaAnt(IExecutionContext context)
        {
            ClassFilesDirectories = CodeCoverageUtilities.ThrowIfParameterEmpty(ClassFilesDirectories, "ClassFilesDirectories");
            ReportDirectory = CodeCoverageUtilities.ThrowIfParameterEmpty(ReportDirectory, "ReportDirectory");
            CCReportTask = CodeCoverageUtilities.ThrowIfParameterEmpty(CCReportTask, "CodeCoverageReportTarget");
            ReportBuildFile = CodeCoverageUtilities.ThrowIfParameterEmpty(ReportBuildFile, "CodeCoverageReportBuildFile");

            CodeCoverageUtilities.ThrowIfClassFilesDirectoriesIsInvalid(ClassFilesDirectories);

            SourceDirectories = CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(context, SourceDirectories, StringUtil.Loc("SourceDirectoriesNotSpecified"));
        }

        public void VerifyInputsForJacocoMaven()
        {
            SummaryFile = CodeCoverageUtilities.ThrowIfParameterEmpty(SummaryFile, "SummaryFile");
            ReportDirectory = CodeCoverageUtilities.ThrowIfParameterEmpty(ReportDirectory, "ReportDirectory");

            CodeCoverageUtilities.ThrowIfClassFilesDirectoriesIsInvalid(ClassFilesDirectories);
        }

        public void VerifyInputsForJacocoGradle()
        {
            ClassFilesDirectories = CodeCoverageUtilities.ThrowIfParameterEmpty(ClassFilesDirectories, "ClassFilesDirectory");
            SummaryFile = CodeCoverageUtilities.ThrowIfParameterEmpty(SummaryFile, "SummaryFile");
            ReportDirectory = CodeCoverageUtilities.ThrowIfParameterEmpty(ReportDirectory, "ReportDirectory");
        }

        public void VerifyInputsForCoberturaGradle()
        {
            ClassFilesDirectories = CodeCoverageUtilities.ThrowIfParameterEmpty(ClassFilesDirectories, "ClassFilesDirectory");
            ReportDirectory = CodeCoverageUtilities.ThrowIfParameterEmpty(ReportDirectory, "ReportDirectory");
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