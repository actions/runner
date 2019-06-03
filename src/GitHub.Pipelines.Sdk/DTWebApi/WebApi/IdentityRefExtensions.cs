using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    internal static class IdentityRefExtensions
    {
        public static IdentityRef Clone(this IdentityRef source)
        {
            if (source == null)
            {
                return null;
            }

            return new IdentityRef
            {
                DisplayName = source.DisplayName,
                Id = source.Id,
                ImageUrl = source.ImageUrl,
                IsAadIdentity = source.IsAadIdentity,
                IsContainer = source.IsContainer,
                ProfileUrl = source.ProfileUrl,
                UniqueName = source.UniqueName,
                Url = source.Url,
            };
        }
    }
}
