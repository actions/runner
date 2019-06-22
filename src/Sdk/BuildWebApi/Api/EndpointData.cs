using System;

namespace GitHub.Build.WebApi
{
    public static class EndpointData
    {
        public const string AcceptUntrustedCertificates = "acceptUntrustedCerts";
        public const string CheckoutNestedSubmodules = "checkoutNestedSubmodules";
        public const string CheckoutSubmodules = "checkoutSubmodules";
        public const string Clean = "clean";
        public const string CleanOptions = "cleanOptions";
        public const string DefaultBranch = "defaultBranch";
        public const string FetchDepth = "fetchDepth";
        public const string GitLfsSupport = "gitLfsSupport";
        public const string JenkinsAcceptUntrustedCertificates = "acceptUntrustedCerts";
        public const string OnPremTfsGit = "onpremtfsgit";
        public const string Password = "password";
        public const string RepositoryId = "repositoryId";
        public const string RootFolder = "rootFolder";
        public const string SkipSyncSource = "skipSyncSource";
        public const string SvnAcceptUntrustedCertificates = "acceptUntrustedCerts";
        public const string SvnRealmName = "realmName";
        public const string SvnWorkspaceMapping = "svnWorkspaceMapping";
        public const string TfvcWorkspaceMapping = "tfvcWorkspaceMapping";
        public const string Username = "username";
    }

    [Obsolete("Use EndpointData instead.")]
    public static class WellKnownEndpointData
    {
        public const string CheckoutNestedSubmodules = EndpointData.CheckoutNestedSubmodules;
        public const string CheckoutSubmodules = EndpointData.CheckoutSubmodules;
        public const string Clean = EndpointData.Clean;
        public const string CleanOptions = EndpointData.CleanOptions;
        public const string DefaultBranch = EndpointData.DefaultBranch;
        public const string FetchDepth = EndpointData.FetchDepth;
        public const string GitLfsSupport = EndpointData.GitLfsSupport;
        public const string JenkinsAcceptUntrustedCertificates = EndpointData.JenkinsAcceptUntrustedCertificates;
        public const string OnPremTfsGit = EndpointData.OnPremTfsGit;
        public const string Password = EndpointData.Password;
        public const string RepositoryId = EndpointData.RepositoryId;
        public const string RootFolder = EndpointData.RootFolder;
        public const string SkipSyncSource = EndpointData.SkipSyncSource;
        public const string SvnAcceptUntrustedCertificates = EndpointData.SvnAcceptUntrustedCertificates;
        public const string SvnRealmName = EndpointData.SvnRealmName;
        public const string SvnWorkspaceMapping = EndpointData.SvnWorkspaceMapping;
        public const string TfvcWorkspaceMapping = EndpointData.TfvcWorkspaceMapping;
        public const string Username = EndpointData.Username;
        public const string AcceptUntrustedCertificates = EndpointData.AcceptUntrustedCertificates;
    }
}
