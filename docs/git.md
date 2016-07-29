# Git

VSTS and TFS require git >= 2.9.  For windows, the agent bundles portable git.  For OSX and Linux, it is a pre-requisite.

## OSX

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

## Ubuntu

If you use git, git >= 2.9.0 is a pre-requisite for Ubuntu agents.

[Install Latest Git on Ubuntu](http://askubuntu.com/questions/568591/how-do-i-install-the-latest-version-of-git-with-apt/568596)

```bash
$ sudo apt-add-repository ppa:git-core/ppa
$ sudo apt-get update
$ sudo apt-get install git
```

## Redhat and CentOS

If you use git, git >= 2.9.0 is a pre-requisite for Redhat/CentOS agents.

[Based on Install Latest Git on Redhat/Centos](http://tecadmin.net/install-git-2-x-on-centos-rhel-and-fedora/#)

```bash
$ yum install curl-devel expat-devel gettext-devel openssl-devel zlib-devel
$ yum install gcc perl-ExtUtils-MakeMaker

$ cd /usr/src
$ wget https://www.kernel.org/pub/software/scm/git/git-2.9.2.tar.gz
$ tar xzf git-2.9.2.tar.gz

$ cd git-2.9.2
$ make prefix=/usr/local/git all
$ make prefix=/usr/local/git install
```

In /etc/bash.rs
```bash
export PATH=$PATH:/usr/local/git/bin
```
