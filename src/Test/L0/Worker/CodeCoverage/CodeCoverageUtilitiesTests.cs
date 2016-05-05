using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageUtilitiesTests
    {
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
