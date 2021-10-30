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
- to run it on your local machine `-P ubuntu-latest=-self-hosted`
- to run it in a docker container `-P ubuntu-latest=catthehacker/ubuntu:act-latest`
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
dotnet publish -c Release --no-self-contained -p:BUILD_OS=Any src/Runner.Client
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
dotnet publish -c Release src/Runner.Client -r win-x64
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

Run the unofficial runner

Linux or macOS:
```
./run.sh
```

Windows:
```
.\run.cmd
```

### Schedule one or more job's

Linux or macOS:
```
./bin/Runner.Client --workflow workflow.yml --event push --payload payload.json --server http://localhost:5000
```

Windows
```
.\bin\Runner.Client.exe --workflow workflow.yml --event push --payload payload.json --server http://localhost:5000
```

Open http://localhost:5000 to see the progress.

### Sample appsetting.json for [try.gitea.io](http://try.gitea.io/)
With this config you are no longer allowed
- to register a runner with any token, you need to specify `--token youNeedToEnterThisTokenToRegisterAnRunner` during configure
- to send anyonymous webhook events and to use `Runner.Client` 

```json
{
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "sqlite": "Data Source=Agents.db;"
  },
  "Runner.Server": {
    "ServerUrl": "https://actions-service.azurewebsites.net",
    "GitServerUrl": "https://try.gitea.io",
    "GitApiServerUrl": "https://try.gitea.io/api/v1",
    "GitGraphQlServerUrl": null,
    "GITHUB_TOKEN": "",
    "WebhookHMACAlgorithmName": "HMACSHA256",
    "WebhookSignatureHeader": "X-Gitea-Signature",
    "WebhookSecret": "youNeedToEnterThisTokenToAuthorizeWebhooks",
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

### Secure Webhook endpoint with a shared secret
For Gitea this should work, if you add `youNeedToEnterThisTokenToAuthorizeWebhooks` as a secret in the configuration page.
```json
{
  "Runner.Server": {
    "WebhookHMACAlgorithmName": "HMACSHA256",
    "WebhookSignatureHeader": "X-Gitea-Signature",
    "WebhookSecret": "youNeedToEnterThisTokenToAuthorizeWebhooks"
  }
}
```

### Change the public url of the Server
If this doesn't match with the you configuration url, you cannot configure any runner.
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
The multiline syntax doesn't work with nektos/act
```
name=value
multilinename<<EOF
First line
Second line
EOF
othername=value2
othername2=value3
```

## Notes
This Software contains reimplementations of some parts of the github server which aren't open source (yet?). 

- matrix parsing
- job parsing
- `on` parsing incl. filter
- api server of the open source client
- context creation
- scheduling
- job inputs / outputs, based on documentation

The following things will behave as expected

- expression evaluation
- step evaluation on the runner
- container actions

## Something not working?
Please open an issue at this fork, to get it fixed.