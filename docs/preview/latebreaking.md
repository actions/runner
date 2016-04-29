# VSTS Agent Preview - Late breaking instructions

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
