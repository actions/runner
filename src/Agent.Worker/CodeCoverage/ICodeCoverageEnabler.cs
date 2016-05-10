namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public interface ICodeCoverageEnabler : IExtension
    {
        /// <summary>
        /// Enables code coverage by editing build file
        /// </summary>
        void EnableCodeCoverage(IExecutionContext context, CodeCoverageEnablerInputs ccInputs);

        /// <summary>
        /// Enabler name. Name convention: CCTool_BuildTool.
        /// </summary>
        string Name { get; }
    }
}