using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Directories
{
    [GenerateSpecificConstants]
    public static class DirectoryName
    {
        /// <summary>
        /// This is a concrete directory.
        /// </summary>
        [GenerateConstant]
        public const string VisualStudioDirectory = "vsd";

        /// <summary>
        /// This is a concrete directory.
        /// </summary>
        [GenerateConstant]
        public const string AzureActiveDirectory = "aad";

        /// <summary>
        /// This is a concrete directory.
        /// </summary>
        public const string ActiveDirectory = "ad";

        /// <summary>
        /// This is a concrete directory.
        /// </summary>
        public const string WindowsMachineDirectory = "wmd";

        /// <summary>
        /// This is a concrete directory.
        /// </summary>
        [GenerateConstant]
        public const string MicrosoftAccount = "msa";

        /// <summary>
        /// This is a concrete directory.
        /// </summary>
        [GenerateConstant]
        public const string GitHub = "ghb";

        /// <summary>
        /// This is a virtual directory that represents the current request context's source directory.
        /// <para>At the deployment level, this directory is equivalent to <see cref="Any"/>.</para>
        /// <para>At the application level or below this directory has the following interpretations:</para>
        /// <para>In an on-prem environment, this is equivalent to <see cref="VisualStudioDirectory"/>.</para>
        /// <para>In a hosted environment with an MSA-backed account, this is equivalent to <see cref="VisualStudioDirectory"/>.</para>
        /// <para>In a hosted environment with an AAD-backed account, this is equivalent to <see cref="AzureActiveDirectory"/>.</para> 
        /// <para>In a hosted environment with accounts created by GitHub users, this is equivalent to <see cref="GitHub"/>.</para> 
        /// </summary>
        public const string SourceDirectory = "src";

        /// <summary>
        /// This is a virtual directory that represents any source.
        /// </summary>
        public const string Any = "any";
    }
}
