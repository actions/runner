using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class RepositoryTypes
    {
        public const String TfsVersionControl = "TfsVersionControl";
        public const String TfsGit = "TfsGit";
        public const String Git = "Git";
        public const String GitHub = "GitHub";
        public const String GitHubEnterprise = "GitHubEnterprise";
        public const String Bitbucket = "Bitbucket";
        public const String Svn = "Svn";
    }

    [Obsolete("Use RepositoryTypes instead.")]
    public static class WellKnownRepositoryTypes
    {
        public const String TfsVersionControl = RepositoryTypes.TfsVersionControl;
        public const String TfsGit = RepositoryTypes.TfsGit;
        public const String Git = RepositoryTypes.Git;
        public const String GitHub = RepositoryTypes.GitHub;
        public const String GitHubEnterprise = RepositoryTypes.GitHubEnterprise;
        public const String Bitbucket = RepositoryTypes.Bitbucket;
        public const String Svn = RepositoryTypes.Svn;
    }
}
