

# ![osx](../res/apple_med.png) OSX System Pre-requisites

## OSX Version

Tested on 10.10 (Yosemite) and 10.11 (El Capitan).  Not domain joined.

## OSX

Net core requires update OpenSSL - [issue 110](https://github.com/Microsoft/vsts-agent/issues/110) 

```bash
$ brew update
$ brew install openssl
$ brew link --force openssl
$ openssl version
OpenSSL 1.0.2f  28 Jan 2016
```

[Discussion here](http://apple.stackexchange.com/questions/126830/how-to-upgrade-openssl-in-os-x)


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

