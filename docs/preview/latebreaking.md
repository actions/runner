# VSTS Agent Preview - Late breaking instructions

## Windows

The minimum supported version is Windows 7.

Even though the agent has no pre-requisites, many of the tasks we run require Visual Studio.

## ![osx](../res/apple_med.png) OSX

Tested on 10.10 (Yosemite) and 11.11 (El Capitan)

Update OpenSSL - [issue 110](https://github.com/Microsoft/vsts-agent/issues/110) 

```bash
$ openssl version
OpenSSL 1.0.2f  28 Jan 2016
```

Instructions [located here](http://apple.stackexchange.com/questions/126830/how-to-upgrade-openssl-in-os-x)
On last check 1.0.2g was the version so ensure you use correct mv, sn commands.  
Restart new terminal after updating.

If you are using TfsVc, install Oracle Java 1.6+.

## ![ubuntu](../res/ubuntu_med.png) Ubuntu 14.04

Before running the agent, you need to:

```bash
sudo apt-get install -y libunwind8 libcurl3 libicu52
```
If you are using TfsVc, install Oracle Java 1.6+.

## ![ubuntu](../res/ubuntu_med.png) Ubuntu 16.04

Before running the agent, you need to:

```bash
sudo apt-get install -y libunwind8 libcurl3

wget http://security.ubuntu.com/ubuntu/pool/main/i/icu/libicu52_52.1-8ubuntu0.2_amd64.deb
sudo dpkg -i libicu52_52.1-8ubuntu0.2_amd64.deb
```
If you are using TfsVc, install Oracle Java 1.6+.

## ![redhat](../res/redhat_med.png) RedHat and CentOS

Install dependencies  
```bash
sudo yum -y install libunwind.x86_64 icu
```
If you are using TfsVc, install Oracle Java 1.6+.