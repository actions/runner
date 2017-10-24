

# ![osx](../res/apple_med.png) OSX System Prerequisites

## OSX Version

### Agent version 2.125.0 or above (.Net Core 2.x)  
  - macOS 10.12 "Sierra" and later versions

.Net Core 2.x doesn't has any extra prerequisites

### Agent version 2.124.0 or below (.Net Core 1.x) 
  - macOS 10.12 "Sierra"
  - macOS 10.11 "El Capitan"
  - macOS 10.10 "Yosemite"

.Net Core 1.x requires update OpenSSL - [issue 110](https://github.com/Microsoft/vsts-agent/issues/110) 

[From Net Core Instructions](https://www.microsoft.com/net/core#macos)

In order to use .NET Core, we first need the latest version of OpenSSL. The easiest way to get this is from [Homebrew](http://brew.sh). After installing brew, do the following:

```bash
brew update
brew install openssl
# Ensure folder exists on machine
mkdir -p /usr/local/lib/
ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/
ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/
```

This was a recent change from brew.  [Issue here](https://github.com/Microsoft/vsts-agent/issues/470)

## Git

If you use git, git >= 2.9.0 is a pre-requisite for OSX agents.

We recommend using [home brew](http://brew.sh) to install

```bash
$ brew update
$ brew install git
==> Downloading https://homebrew.bintray.com/bottles/git-2.9.0.el_capitan.bottle.tar.gz
...
(restart terminal)
$ git --version
git version 2.9.0
$ which git
/usr/local/bin/git
$ ls -la /usr/local/bin/git
... /usr/local/bin/git -> ../Cellar/git/2.9.0/bin/git
```

## Optionally Java if TfsVc

The agent distributes team explorer everywhere.

But, if you are using TfsVc, install Oracle Java 1.6+ as TEE uses Java.

