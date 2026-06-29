using System.Collections.Generic;

namespace GitHub.DistributedTask.Logging
{
    public interface ISecretRegistrationNotifier
    {
        void NotifySecretRegistration(List<string> secretValues, List<string> secretRegexes);
    }

    public sealed class NoOpSecretRegistrationNotifier : ISecretRegistrationNotifier
    {
        public static readonly NoOpSecretRegistrationNotifier Instance = new NoOpSecretRegistrationNotifier();

        private NoOpSecretRegistrationNotifier()
        {
        }

        public void NotifySecretRegistration(List<string> secretValues, List<string> secretRegexes)
        {
            // Intentionally empty. A concrete API caller can be wired later.
        }
    }
}
