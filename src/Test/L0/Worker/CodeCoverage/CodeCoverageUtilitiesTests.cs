using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageUtilitiesTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private List<string> _outputMessages = new List<string>();

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void GetPriorityOrderTest()
        {
            Assert.Equal(1, CodeCoverageUtilities.GetPriorityOrder("cLaSs"));
            Assert.Equal(2, CodeCoverageUtilities.GetPriorityOrder("ComplexiTy"));
            Assert.Equal(3, CodeCoverageUtilities.GetPriorityOrder("MEthoD"));
            Assert.Equal(4, CodeCoverageUtilities.GetPriorityOrder("line"));
            Assert.Equal(5, CodeCoverageUtilities.GetPriorityOrder("InstruCtion"));
            Assert.Equal(6, CodeCoverageUtilities.GetPriorityOrder("invalid"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void CopyFilesWithDirectoryStructureWhenInputIsNull()
        {
            string destinationFilePath = string.Empty;
            CodeCoverageUtilities.CopyFilesFromFileListWithDirStructure(null, ref destinationFilePath);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void CopyFilesWithDirectoryStructureWhenFilesWithSameNamesAreGiven()
        {
            List<string> files = GetAdditionalCodeCoverageFilesWithSameFileName();
            string destinationFilePath = Path.Combine(Path.GetTempPath(), "additional");
            try
            {
                Directory.CreateDirectory(destinationFilePath);
                CodeCoverageUtilities.CopyFilesFromFileListWithDirStructure(files, ref destinationFilePath);
                Assert.True(File.Exists(Path.Combine(destinationFilePath, "A/a.xml")));
                Assert.True(File.Exists(Path.Combine(destinationFilePath, "B/a.xml")));
                Assert.True(File.Exists(Path.Combine(destinationFilePath, "C/b.xml")));
                Assert.True(File.Exists(Path.Combine(destinationFilePath, "a.xml")));
            }
            finally
            {
                Directory.Delete(destinationFilePath, true);
                Directory.Delete(Path.Combine(Path.GetTempPath(), "A"), true);
                Directory.Delete(Path.Combine(Path.GetTempPath(), "B"), true);
                Directory.Delete(Path.Combine(Path.GetTempPath(), "C"), true);
                File.Delete(Path.Combine(Path.GetTempPath(), "a.xml"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void CopyFilesWithDirectoryStructureWhenFilesWithDifferentNamesAreGiven()
        {
            List<string> files = GetAdditionalCodeCoverageFilesWithDifferentFileNames();
            string destinationFilePath = Path.Combine(Path.GetTempPath(), "additional");
            try
            {
                Directory.CreateDirectory(destinationFilePath);
                CodeCoverageUtilities.CopyFilesFromFileListWithDirStructure(files, ref destinationFilePath);
                Assert.True(File.Exists(Path.Combine(destinationFilePath, "a.xml")));
                Assert.True(File.Exists(Path.Combine(destinationFilePath, "b.xml")));
            }
            finally
            {
                Directory.Delete(destinationFilePath, true);
                Directory.Delete(Path.Combine(Path.GetTempPath(), "A"), true);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void ThrowsIfParameterNull()
        {
            Assert.Throws<ArgumentException>(() => CodeCoverageUtilities.ThrowIfParameterEmpty(null, "inputName"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void ThrowsIfParameterIsWhiteSpace()
        {
            Assert.Throws<ArgumentException>(() => CodeCoverageUtilities.ThrowIfParameterEmpty("       ", "inputName"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void SetSourceDirectoryToCurrentLogsMessage()
        {
            SetupMocks();
            CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(_ec.Object, " ", "warningMessage");
            Assert.Equal(0, _warnings.Count);
            Assert.Equal(0, _errors.Count);
            Assert.Equal(1, _outputMessages.Count);
            Assert.Equal(_outputMessages[0], "warningMessage");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void SetSourceDirectoryTrimsSourceDirectory()
        {
            SetupMocks();
            var sourceDir = CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(_ec.Object, " sourceDir  ", "warningMessage");
            Assert.Equal(0, _warnings.Count);
            Assert.Equal(0, _errors.Count);
            Assert.Equal(0, _outputMessages.Count);
            Assert.Equal("sourceDir", sourceDir);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void GetFiltersWithInvalidFilterInput()
        {
            string include, exclude;
            Assert.Throws<ArgumentException>(() => CodeCoverageUtilities.GetFilters("invalidFilter", out include, out exclude));
            Assert.Throws<ArgumentException>(() => CodeCoverageUtilities.GetFilters("+,-:", out include, out exclude));
            Assert.Throws<ArgumentException>(() => CodeCoverageUtilities.GetFilters("+: , -: ", out include, out exclude));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void GetFiltersWithValidFilterInput()
        {
            string include, exclude;
            CodeCoverageUtilities.GetFilters("", out include, out exclude);
            Assert.Equal(include, "");
            Assert.Equal(exclude, "");

            CodeCoverageUtilities.GetFilters("+:avfd.s.sdsd,-:sad.fdf.fs,-:aa.bb,+:av.fd", out include, out exclude);
            Assert.Equal(include, "avfd.s.sdsd:av.fd");
            Assert.Equal(exclude, "sad.fdf.fs:aa.bb");

            CodeCoverageUtilities.GetFilters("+:avfd.s.sdsd,-:sad.fdf.fs", out include, out exclude);
            Assert.Equal(include, "avfd.s.sdsd");
            Assert.Equal(exclude, "sad.fdf.fs");
        }

        private void SetupMocks()
        {
            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>
                ((tag, message) =>
                {
                    _outputMessages.Add(message);
                });

            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>()))
            .Callback<Issue>
            ((issue) =>
            {
                if (issue.Type == IssueType.Warning)
                {
                    _warnings.Add(issue.Message);
                }
                else if (issue.Type == IssueType.Error)
                {
                    _errors.Add(issue.Message);
                }
            });
        }

        private List<string> GetAdditionalCodeCoverageFilesWithSameFileName()
        {
            var files = new List<string>();
            files.Add(Path.Combine(Path.GetTempPath(), "A/a.xml"));
            files.Add(Path.Combine(Path.GetTempPath(), "B/a.xml"));
            files.Add(Path.Combine(Path.GetTempPath(), "C/b.xml"));
            files.Add(Path.Combine(Path.GetTempPath(), "a.xml"));
            foreach (var file in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, "Test");
            }
            return files;
        }

        private List<string> GetAdditionalCodeCoverageFilesWithDifferentFileNames()
        {
            var files = new List<string>();
            files.Add(Path.Combine(Path.GetTempPath(), "A/a.xml"));
            files.Add(Path.Combine(Path.GetTempPath(), "A/b.xml"));
            foreach (var file in files)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, "Test");
            }
            return files;
        }
    }
}
