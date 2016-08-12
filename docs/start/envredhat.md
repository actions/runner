

# ![redhat](../res/redhat_med.png) Redhat/CentOS System Pre-requisites

## Versions

Tested on Redhat 7.1.  Not domain joined.

## Dependency Packages

```bash
sudo yum -y install libunwind.x86_64 icu
```
If you're still having issues:
[Full List Needed](https://github.com/dotnet/core/blob/master/Documentation/prereqs.md)

## Git

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

In /etc/bashrc
```bash
export PATH=$PATH:/usr/local/git/bin
```

## Optionally Java if TfsVc

The agent distributes team explorer everywhere.

But, if you are using TfsVc, install Oracle Java 1.6+ as TEE uses Java.

