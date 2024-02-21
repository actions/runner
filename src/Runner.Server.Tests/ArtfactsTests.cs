using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Github.Actions.Results.Api.V1;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Runner.Server.Models;

namespace Runner.Server.Tests;

public class ArtfactsTests 
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Job job;

    public ArtfactsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        job = new Job() {
            JobId = Guid.NewGuid(),
            runid = 1,
            WorkflowRunAttempt = new WorkflowRunAttempt() {
                ArtifactsMinAttempt = 1,
                Attempt = 1,
                WorkflowRun = new WorkflowRun {
                }
            }
        };
        using(var scope = _factory.Services.CreateScope())
        using(var db = scope.ServiceProvider.GetRequiredService<SqLiteDb>()) {
            db.Jobs.Add(job);
            db.Artifacts.Add(new ArtifactContainer() { Attempt = job.WorkflowRunAttempt } );
            db.SaveChanges();
            job.runid = job.WorkflowRunAttempt.WorkflowRun.Id;
            db.SaveChanges();
        }
    }

    private JsonFormatter formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation().WithPreserveProtoFieldNames(true).WithFormatDefaultValues(false));

    [Fact]
    public async Task CreateArtfact()
    {
        // Arrange
        var client = _factory.CreateClient();
        string stoken = GetJobAuthToken();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", stoken); //"Bearer " + stoken;
        // Act
        var response = await client.PostAsync("twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact", new StringContent(formatter.Format(new CreateArtifactRequest
        {
            Name = "test",
            WorkflowJobRunBackendId = job.JobId.ToString(),
            Version = 4,
            WorkflowRunBackendId = job.runid.ToString()
        }), Encoding.UTF8, "application/json"));


        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var resp = CreateArtifactResponse.Parser.ParseJson(await response.Content.ReadAsStringAsync());

        Assert.True(resp.Ok, "Ok response");
        Assert.NotNull(resp.SignedUploadUrl);
    }

    [Fact]
    public async Task CreateArtfactAfterDelete()
    {
        // Arrange
        var client = _factory.CreateClient();
        string stoken = GetJobAuthToken();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", stoken); //"Bearer " + stoken;
        // Act
        var response = await client.PostAsync("twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact", new StringContent(formatter.Format(new CreateArtifactRequest
        {
            Name = "test",
            WorkflowJobRunBackendId = job.JobId.ToString(),
            Version = 4,
            WorkflowRunBackendId = job.runid.ToString()
        }), Encoding.UTF8, "application/json"));


        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var resp = CreateArtifactResponse.Parser.ParseJson(await response.Content.ReadAsStringAsync());

        Assert.True(resp.Ok, "Ok response");
        Assert.NotNull(resp.SignedUploadUrl);

        response = await client.PostAsync("twirp/github.actions.results.api.v1.ArtifactService/DeleteArtifact", new StringContent(formatter.Format(new DeleteArtifactRequest
        {
            Name = "test",
            WorkflowJobRunBackendId = job.JobId.ToString(),
            WorkflowRunBackendId = job.runid.ToString()
        }), Encoding.UTF8, "application/json"));

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var delresp = DeleteArtifactResponse.Parser.ParseJson(await response.Content.ReadAsStringAsync());

        Assert.True(delresp.Ok, "Ok response");

        response = await client.PostAsync("twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact", new StringContent(formatter.Format(new CreateArtifactRequest
        {
            Name = "test",
            WorkflowJobRunBackendId = job.JobId.ToString(),
            Version = 4,
            WorkflowRunBackendId = job.runid.ToString()
        }), Encoding.UTF8, "application/json"));


        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        resp = CreateArtifactResponse.Parser.ParseJson(await response.Content.ReadAsStringAsync());

        Assert.True(resp.Ok, "Ok response");
        Assert.NotNull(resp.SignedUploadUrl);
    }

    [Fact]
    public async Task CreateArtfactTwice()
    {
        // Arrange
        var client = _factory.CreateClient();
        string stoken = GetJobAuthToken();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", stoken); //"Bearer " + stoken;
        // Act
        var response = await client.PostAsync("twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact", new StringContent(formatter.Format(new CreateArtifactRequest
        {
            Name = "test",
            WorkflowJobRunBackendId = job.JobId.ToString(),
            Version = 4,
            WorkflowRunBackendId = "1"
        }), Encoding.UTF8, "application/json"));


        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var resp = CreateArtifactResponse.Parser.ParseJson(await response.Content.ReadAsStringAsync());

        Assert.True(resp.Ok, "Ok response");
        Assert.NotNull(resp.SignedUploadUrl);

        // Act
        response = await client.PostAsync("twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact", new StringContent(formatter.Format(new CreateArtifactRequest
        {
            Name = "test",
            WorkflowJobRunBackendId = job.JobId.ToString(),
            Version = 4,
            WorkflowRunBackendId = job.runid.ToString()
        }), Encoding.UTF8, "application/json"));

        resp = CreateArtifactResponse.Parser.ParseJson(await response.Content.ReadAsStringAsync());

        Assert.False(resp.Ok, "Not Ok response");
    }

    private string GetJobAuthToken()
    {
        var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);

        var myIssuer = "http://githubactionsserver";
        var myAudience = "http://githubactionsserver";

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim("Agent", "job"),
                new Claim("repository", "test/test"),
                new Claim("ref", "refs/heads/test"),
                new Claim("defaultRef", "refs/heads/main"),
                new Claim("attempt", "1"),
                new Claim("artifactsMinAttempt", "1"),
                new Claim("localcheckout", ""),
                new Claim("runid", "1"),
                new Claim("github_token", ""),
                new Claim("scp", $"Actions.Results:{job.runid}:{job.JobId}")
            }),
            Expires = DateTime.UtcNow.AddMinutes(10),
            Issuer = myIssuer,
            Audience = myAudience,
            SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var stoken = tokenHandler.WriteToken(token);
        return stoken;
    }
}