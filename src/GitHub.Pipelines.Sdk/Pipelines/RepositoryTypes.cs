using System;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    public static class RepositoryTypes
    {
        public static readonly String Bitbucket = nameof(Bitbucket);
        public static readonly String ExternalGit = nameof(ExternalGit);
        public static readonly String Git = nameof(Git);
        public static readonly String GitHub = nameof(GitHub);
        public static readonly String GitHubEnterprise = nameof(GitHubEnterprise);
        public static readonly String Tfvc = nameof(Tfvc);
        public static readonly String Svn = nameof(Svn);
    }
}
