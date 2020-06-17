using Newtonsoft.Json;

namespace GitHub.Runner.Worker
{
  public static class IStepExtensions
  {
    public static string GetRefName(this IStep step, string defaultRefName = null)
    {
      // TODO: Really check a feature flag.
      if (s_featureFlagEnabled)
      {
        if (step is JobExtensionRunner extensionRunner && extensionRunner.RepositoryRef != null)
        {
          return JsonConvert.SerializeObject(extensionRunner.RepositoryRef);
        }

        if (step is IActionRunner actionRunner && actionRunner.Action?.Reference != null)
        {
          return JsonConvert.SerializeObject(actionRunner.Action.Reference);
        }

        // RefName should always be valid json or null.
        return null;
      }

      return defaultRefName;
    }

    private static bool s_featureFlagEnabled = true;
  }
}