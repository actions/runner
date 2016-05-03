# VSTS Agent Preview - Late breaking instructions

## OSX

Tested on 10.10 (Yosemite) and 11.11 (El Capitan)

Update OpenSSL - [issue 110](https://github.com/Microsoft/vsts-agent/issues/110) 

```bash
$ openssl version
OpenSSL 1.0.2f  28 Jan 2016
```

Instructions [located here](http://apple.stackexchange.com/questions/126830/how-to-upgrade-openssl-in-os-x)
On last check 1.0.2g was the version so ensure you use correct mv, sn commands.  
Restart new terminal after updating.

## Ubuntu 14.04

Before running the agent, you need to:

```bash
sudo apt-get install -y libunwind8 libcurl3 libicu52
```

## Ubuntu 16.04

Before running the agent, you need to:

```bash
sudo apt-get install -y libunwind8 libcurl3

wget http://security.ubuntu.com/ubuntu/pool/main/i/icu/libicu52_52.1-8ubuntu0.2_amd64.deb
sudo dpkg -i libicu52_52.1-8ubuntu0.2_amd64.deb
```

## RedHat and CentOS

Not yet supported or tested in this preview
