using System;

namespace GitHub.Services.GitHubConnector
{
    public class GitHubAccount
    {
        public GitHubAccount(
            string id,
            GitHubAccountType accountType, 
            string login, 
            string url, 
            string avatarUrl,
            string description)
        {
            Id = id;
            AccountType = accountType;
            Login = login;
            Url = url;
            AvatarUrl = avatarUrl;
            Description = description;
        }

        public GitHubAccount() { }

        public string Id { get; set; }

        public GitHubAccountType AccountType { get; set; }

        public string Login { get; set; }

        public string Url { get; set; }

        public string AvatarUrl { get; set; }

        public string Description { get; set; }
    }
}
