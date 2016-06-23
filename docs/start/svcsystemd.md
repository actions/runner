# Running As A Service On Unix with SystemD

Key Points:
  - This is a convenience which only creates OS specific service files.
  - SystemD is used on Linux.  Ubuntu 16 LTS, Redhat 7.1 has SystemD
  - SystemD commands require sudo

## Managing the Service

./svc.sh was generated to manage your service

## Install

Install will create a systemd unit file on Linux

```bash
$ sudo ./svc.sh install
...
Creating runsvc.sh
Creating .Service
svc install complete
```

Service files point to `./runsvc.sh` which will setup the environment and start the agents host.  See Environment section below.

### Start
```bash
$ sudo ./svc.sh start
```

### Status
```bash
$ sudo ./svc.sh status
```

### Stop
```bash
$ sudo ./svc.sh stop
```

### Uninstall
```bash
$ sudo ./svc.sh uninstall

```

## Setting the Environment

When you install and/or configure tools, your path is often setup or other environment variables are set.  Examples are PATH, JAVA_HOME, ANT_HOME, MYSQL_PATH etc...

If your environment changes at any time, you can run env.sh and it will update your path.  You can also manually edit .env file.  Changes are retained. 

Stop and start the service for changes to take effect.

```bash
$ ./env.sh 
$ sudo ./svc.sh stop
...

$ sudo ./svc.sh start
...
```

## Environment

Configuring as a service will snapshot off your PATH and other "interesting variables" like LANG, JAVA_HOME etc..  When the service starts, it will read these and set.  This allows for unified environment management between

```bash
$ ls -la
-rwxrwx---    1 bryanmac  staff   189 May 29 11:42 .agent
-rwxrwx---    1 bryanmac  staff   106 May 29 11:41 .credentials
-rw-r--r--    1 bryanmac  staff    58 May 29 11:44 .env
-rw-r--r--    1 bryanmac  staff   187 May 29 11:40 .path
...
-rwxr-xr-x    1 bryanmac  staff   546 May 29 11:40 env.sh
```

Run ./env.sh to update.

You can also inject anything you want to run when the service runs.  For example setting up environment, calling other scripts etc...

./runsvc.sh
```
# insert anything to setup env when running as a service
```

## Service Files

This is a convenience that simply creates a service file for you can sets permissions.  You are free to manually configure and control your service using alternate methods.

Details are in .service file in root of the agent

Linux SystemD: /etc/systemd/system/vsts.agent.{accountName}.{agentName}.service

These files are created from a template located

Linux: ./bin/vsts.agent.service.template
