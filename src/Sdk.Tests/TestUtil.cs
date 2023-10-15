namespace Sdk.Tests
{
    internal class TestUtil
    {
        public static string GetTestWorkflowDirectory()
        {
            // assumes working directory is src/sdk.tests/bin/debug/platform
            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../testworkflows/")); 
            if (!Directory.Exists(basePath)) {
                throw new DirectoryNotFoundException(basePath);
            }
            return basePath;
        }

        public static string GetAzPipelineFolder(string folder = "")
        {
            var basePath = GetTestWorkflowDirectory();
            return Path.GetFullPath(Path.Combine(basePath, "azpipelines", folder));
        }
    }
}
