

# ![osx](../res/apple_med.png) macOS/OS X System Prerequisites

## macOS/OS X Version

### Agent version 2.125.0 or above (.Net Core 2.x)  
  - macOS Sierra (10.12) and later versions

.Net Core 2.x doesn't has any extra prerequisites

### Agent version 2.124.0 or below (.Net Core 1.x) 
  - macOS Sierra (10.12)
  - OS X El Capitan (10.11)
  - OS X Yosemite (10.10)

.Net Core 1.x requires you to update OpenSSL - [issue 110](https://github.com/Microsoft/vsts-agent/issues/110) 

[From Net Core Instructions](https://www.microsoft.com/net/core#macos)

In order to use .NET Core, we first need a newer version of OpenSSL. The easiest way to get this is from [Homebrew](https://brew.sh). After installing Homebrew, run the following:

```bash
brew install openssl
# Ensure folder exists on machine
mkdir -p /usr/local/lib/
ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/
ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/
```

This was a change in Homebrew. [Issue here](https://github.com/Microsoft/vsts-agent/issues/470)

## Git

If you use OS X Yosemite agents git >= 2.9.0 is a pre-requisite and not provided by OS X (OS X El Capitan provides Git 2.10.1).

We recommend using [Homebrew](https://brew.sh) to install Git:

```bash
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

But, if you are using TfsVc, install Oracle [Java SE Development Kit](http://www.oracle.com/technetwork/java/javaseproducts/downloads/index.html) (JDK) 1.6+. 
> Notes:  
> 1. Only install JRE is not sufficient.  
> 2. Don't use OpenJDK, use Oracle JDK.  

## [More .Net Core Prerequisites Information](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)
