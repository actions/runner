using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    public static class BuildDefinitionExtensions
    {
        public static T GetProcess<T>(
            this BuildDefinition definition) where T : BuildProcess
        {
            ArgumentUtility.CheckForNull(definition, nameof(definition));
            ArgumentUtility.CheckForNull(definition.Process, nameof(definition.Process));
            ArgumentUtility.CheckType<T>(definition.Process, nameof(definition.Process), nameof(T));

            return definition.Process as T;
        }
    }
}
