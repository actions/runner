using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runner.Server.Controllers
{
    public class GitHubAppIntegrationBase : VssControllerBase {

        protected string GitHubAppPrivateKeyFile { get; }
        protected int GitHubAppId { get; }
        private string GitServerUrl { get; }
        
        protected GitHubAppIntegrationBase(IConfiguration configuration) : base(configuration) {
            GitHubAppPrivateKeyFile = configuration.GetSection("Runner.Server")?.GetValue<string>("GitHubAppPrivateKeyFile") ?? "";
            GitHubAppId = configuration.GetSection("Runner.Server")?.GetValue<int>("GitHubAppId") ?? 0;
            GitServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitServerUrl") ?? "";
        }

        protected async Task<string> CreateGithubAppToken(string repository_name, object payload = null) {
            if(!string.IsNullOrEmpty(GitHubAppPrivateKeyFile) && GitHubAppId != 0) {
                try {
                    var ownerAndRepo = repository_name.Split("/", 2);
                    // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
                    var generator = new GitHubJwt.GitHubJwtFactory(
                        new GitHubJwt.FilePrivateKeySource(GitHubAppPrivateKeyFile),
                        new GitHubJwt.GitHubJwtFactoryOptions
                        {
                            AppIntegrationId = GitHubAppId, // The GitHub App Id
                            ExpirationSeconds = 500 // 10 minutes is the maximum time allowed
                        }
                    );
                    var jwtToken = generator.CreateEncodedJwtToken();
                    // Pass the JWT as a Bearer token to Octokit.net
                    var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"), new Uri(GitServerUrl))
                    {
                        Credentials = new Octokit.Credentials(jwtToken, Octokit.AuthenticationType.Bearer)
                    };
                    var installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(ownerAndRepo[0], ownerAndRepo[1]);
                    var response = await appClient.Connection.Post<Octokit.AccessToken>(Octokit.ApiUrls.AccessTokens(installation.Id), payload ?? (object) new { Permissions = new { metadata = "read", contents = "read" } }, Octokit.AcceptHeaders.GitHubAppsPreview, Octokit.AcceptHeaders.GitHubAppsPreview);
                    return response.Body.Token;
                } catch {

                }
            }
            return null;
        }

        protected async Task DeleteGithubAppToken(string token) {
            var appClient2 = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"), new Uri(GitServerUrl))
            {
                Credentials = new Octokit.Credentials(token)
            };
            await appClient2.Connection.Delete(new Uri("installation/token", UriKind.Relative));
        }
    }
}