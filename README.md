# GitHub Actions Runner + Server

[![Runner CI](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml/badge.svg)](https://github.com/ChristopherHX/runner.server/actions/workflows/build.yml)

This fork adds two executables to this Project, `Runner.Server` as a runner backend like github actions and `Runner.Client` to schedule workflows via commandline.

<p align="center">
  <img src="src/Runner.Server/webpage1.png">
</p>

## Usage
- [Download the Actions Runner Client + Server](https://github.com/ChristopherHX/runner/releases/latest)
- The installation directory needs to be accessible by docker file sharing
  - On linux (Docker) all non overlayfs folders should work
  - On macOs (Docker Desktop) you might need to add the install path to Docker File Sharing
  - On windows (Docker Desktop) you might need to accept all file sharing requests (hyper-v backend)
  - Docker Settings -> Resources -> File Sharing
  - [Tracking issue for macOS](https://github.com/ChristopherHX/runner.server/issues/72)
- Clone your github actions repo
- Run `Runner.Client`(`.exe`) (It is inside the bin folder of the package) inside your checkout

### Dotnet Tool (gharun)
The new nuget package [can be found here](https://www.nuget.org/packages/io.github.christopherhx.gharun)
- Install the dotnet sdk 5.0.x (https://dotnet.microsoft.com/download/dotnet/5.0)
- `dotnet tool install --global io.github.christopherhx.gharun`
- Run `gharun` like `Runner.Client`

## Troubleshooting

If you get an error like: 
```
Error: No runner is registered for the requested runs-on labels: [ubuntu-latest], please register and run a self-hosted runner with at least these labels...
```

Then you will need to add one of the following cli options, replace `ubuntu-latest` with the content between `runs-on labels: [` The labels here without spaces `]`
- to run it on your local machine e.g. `-P ubuntu-latest=-self-hosted`, `-P self-hosted,linux,mylabel=-self-hosted`
- to run it in a docker container e.g. `-P ubuntu-latest=catthehacker/ubuntu:act-latest`, `-P self-hosted,linux,mylabel=catthehacker/ubuntu:act-latest`
  For more docker images refer to https://github.com/nektos/act#runners

This Software reads [act configuration files](https://github.com/nektos/act#configuration), you can save this inside a `.actrc` in your current or home folder to avoid typing it in your commandline.

## Building

```
dotnet msbuild src/dir.proj -t:GenerateConstant
dotnet build src/Runner.Client
```

This builds `Runner.Client`, `Runner.Server` and a modifed github actions runner `Runner.Listener`.

### Building a framework dependent and os independent executable
```
dotnet msbuild src/dir.proj -t:GenerateConstant
dotnet publish src/Runner.Client -c Release --no-self-contained -p:BUILD_OS=Any -p:RuntimeFrameworkVersion=5.0.0
```

#### To run the package on a different Operating System
```
dotnet Runner.Client.dll
```
```
dotnet Runner.Server.dll
```
```
dotnet Runner.Listener.dll
```
### Building a self-contained executable
Replace `win-x64` with any supported platform of .net5: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog.
```
dotnet msbuild src/dir.proj -t:GenerateConstant
dotnet publish src/Runner.Client -c Release -r win-x64
```
#### To run the package
```
./Runner.Client
```
```
./Runner.Server
```
```
./Runner.Listener
```

### Building a dotnet tool
```
dotnet msbuild src/dir.proj -t:GenerateConstant
dotnet pack src/Runner.Client -c Release -p:BUILD_OS=Any -p:RuntimeFrameworkVersion=5.0.0
```
#### To install the package
```
dotnet tool install -g io.github.christopherhx.gharun --add-source src/Runner.Client/nupkg
```
#### To run the package
```
gharun
```

## Advanced Usage

You may need to allow non root processes to bind port 80 on Linux https://superuser.com/questions/710253/allow-non-root-process-to-bind-to-port-80-and-443 otherwise you cannot register official runners. If you configure the runner of this project any port is fine, e.g. port 5000 will work too.
```
./bin/Runner.Server --urls http://localhost
```

### Setup a runner
You can type anything you want for registration and removal token authentication isn't implemented yet.

Linux or macOS:
```
./config.sh --unattended --url http://localhost/runner/server --token "ThisIsIgnored"
```

Windows:
```
.\config.cmd --unattended --url http://localhost/runner/server --token "ThisIsIgnored"
```

Connect a runner

Linux or macOS:
```
./run.sh
```

Windows:
```
.\run.cmd
```

### Schedule one or more job's
You will have to remove any leading `/` from your server url.

Linux or macOS:
```
./bin/Runner.Client --workflow workflow.yml --event push --payload payload.json --server http://localhost
```

Windows
```
.\bin\Runner.Client.exe --workflow workflow.yml --event push --payload payload.json --server http://localhost
```

Or send github / gitea webhooks to http://localhost/runner/server/_apis/v1/Message.

Open http://localhost to see the progress.

### Sample appsettings.json for [try.gitea.io](http://try.gitea.io/)

```json
{
  "AllowedHosts": "*",
  "Runner.Server": {
    "ServerUrl": "https://actions-service.azurewebsites.net",
    "GitServerUrl": "https://try.gitea.io",
    "GitApiServerUrl": "https://try.gitea.io/api/v1",
    "GitGraphQlServerUrl": null,
    "GITHUB_TOKEN": "",
    "ActionDownloadUrls": [
      {
        "TarballUrl": "https://try.gitea.io/{0}/archive/{1}.tar.gz",
        "ZipballUrl": "https://try.gitea.io/{0}/archive/{1}.zip"
      }
    ]
  }
}
```

### Secure the runner registration endpoint
With this config you are no longer allowed to register a runner with any token, you need to specify `--token youNeedToEnterThisTokenToRegisterAnRunner` during configure
```json
{
  "Runner.Server": {
    "RUNNER_TOKEN": "youNeedToEnterThisTokenToRegisterAnRunner"
  }
}
```

### Allow PullRequest events
Process the `pull_request` action trigger, if disabled only `pull_request_target` from the target branch are processed. Enabling this make it possible to leak secrets and run arbitary code on your self-hosted runners. Proper secret and self-hosted runner protection needs to be implemented, to make this save to enable.
```json
{
  "Runner.Server": {
    "AllowPullRequests": true
  }
}
```

### Secure Webhook endpoint with a shared secret
Add `youNeedToEnterThisTokenToAuthorizeWebhooks` as a secret in the configuration page.

#### For Gitea this should work
```json
{
  "Runner.Server": {
    "WebhookHMACAlgorithmName": "HMACSHA256",
    "WebhookSignatureHeader": "X-Gitea-Signature",
    "WebhookSecret": "youNeedToEnterThisTokenToAuthorizeWebhooks"
  }
}
```
#### For GitHub this should work
```json
{
  "Runner.Server": {
    "WebhookHMACAlgorithmName": "HMACSHA256",
    "WebhookSignatureHeader": "X-Hub-Signature-256",
    "WebhookSignaturePrefix": "sha256=",
    "WebhookSecret": "youNeedToEnterThisTokenToAuthorizeWebhooks"
  }
}
```

### Change the public url of the Server
If this doesn't match with the your configuration url, you cannot configure any runner.
```json
{
  "Runner.Server": {
    "ServerUrl": "https://actions-service.azurewebsites.net",
  }
}
```

### Configure insecure Secrets or feature toggles on the Server
```json
{
  "Runner.Server": {
    "Secrets": [
      {"Name": "mysecret1", "Value": "test"},
      {"Name": "myothersecret", "Value": "other"}
    ]
  }
}
```

### Configure to use sqlite instead of an in Memory DB
```json
{
  "ConnectionStrings": {
    "sqlite": "Data Source=Agents.db;"
  }
}
```

### OpenId Connect for gitea
Currently only requires login if configured.
You will need a pem certificate pair or choose another aspnetcore https configuration
- cert.pem: only a single certificate will work, no cert chain
- key.pem

Add `<url of Runner.Server>/signin-oidc` (https://localhost:5001/signin-oidc) as redirect url for the OAuth app in gitea.
```json
{
  "Kestrel": {
    "Endpoints": {
      "HttpsFromPem": {
        "Url": "https://*:5001",
        "Certificate": {
          "Path": "./cert.pem",
          "KeyPath": "./key.pem"
        }
      }
    }
  },
  "ClientId": "ClientId of your Oauth app",
  "ClientSecret": "Client secret of your Oauth app",
  "Authority": "https://try.gitea.io",
}
```

### The `.actrc` File
Put every parameter pair into a single line, here just a sample
```
-e event.json
--env-file myenvs
--secret-file mysecrets
-P self-hosted,linux=-self-hosted
-P ubuntu-latest=catthehacker/ubuntu:act-latest
-P ubuntu-20.04=node:12.20.1-buster-slim
-P ubuntu-18.04=node:12.20.1-buster-slim
-P ubuntu-16.04=node:12.20.1-stretch-slim
```

### The env-file and secret-file
This multiline syntax doesn't work with nektos/act and vice versa.
```
name=value
multilinename<<EOF
First line
Second line
EOF
othername=value2
othername2=value3
```

### Dev
```
dotnet build ./src/Runner.Server/ /p:EFMigration=ON
dotnet ef migrations add --project ./src/Runner.Server/ --no-build PersistentJobs
dotnet pack src/Runner.Client -c Release -p:BUILD_OS=Any -p:RuntimeFrameworkVersion=5.0.0 -p:Version=3.4.0.3
dotnet tool update -g io.github.christopherhx.gharun --add-source src/Runner.Client/nupkg
```

## Notes
This Software contains Open Source reimplementations of some parts of the proprietary github action service.

- manage runners
- job parsing and scheduling to runners
- matrix parsing and evaluation
- callable workflows
- `on` parsing incl. filter
- context creation of `github`, `needs`, `matrix` and `strategy`
- job inputs / outputs, based on documentation
- secret management
- cache service
- artifact service

The following things will behave exactly like the original

- expression evaluation
- step evaluation on the runner incl. container actions

## Something not working?
Please open an issue at this fork, to get it fixed.