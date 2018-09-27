

# ![Ubuntu](../res/ubuntu_med.png) Ubuntu System Prerequisites [2.124.0 or below]

## Versions

Tested on 18.04 LTS (Bionic), 16.04 LTS (Xenial) and 14.04 LTS (Trusty).  Not domain joined.  

18.04 is recommended since it's the latest and supports SystemD for running as a service.

## Dependency Packages

### Ubuntu 18.04 (x64, ARM32), 16.04 (x64 only)
```bash
sudo apt-get install -y libunwind8 libcurl3
```

### Ubuntu 14.04 (x64 only)
```bash
sudo apt-get install -y libunwind8 libcurl3 libicu52
```

If you're still having issues:
[Full List Needed](https://github.com/dotnet/core/blob/master/Documentation/prereqs.md)

## Git

If you use git, git >= 2.9.0 is a pre-requisite for Ubuntu agents.

[Install Latest Git on Ubuntu](http://askubuntu.com/questions/568591/how-do-i-install-the-latest-version-of-git-with-apt/568596)

```bash
$ sudo apt-add-repository ppa:git-core/ppa
$ sudo apt-get update
$ sudo apt-get install git
```

## Optionally Java if using TFVC

The agent distributes Team Explorer Everywhere.

But, if you are using TFVC, install Oracle Java 1.8+ as TEE uses Java.

## Etc

There was an assertion that on Ubuntu 16 this was needed.  We didn't need.  Adding in case it helps someone.  We will verify on clean build and dev boxes.

```bash
apt-get install libcurl4-openssl-dev
```

