# Running As A Service On Unix and OSX

## Using Your Path

If you install dev tools and have customized your $PATH, you can snapshot it for the service to use.

```bash
echo $PATH > .Path
```

You can do this before config, after config (restart service), or after you install various tools (restart service).

## Configuration

**During configuration, answer Y to run as a service**.  

The systemd and launchagent files run ./runsvc.sh which will set PATH to that if it's present.

You can also inject anything you want to run when the service runs.  For example setting up environment, calling other scripts etc...

./runsvc.sh
```
# insert anything to setup env when running as a service
```

Start and stop the service after making changes

## Managing the Service

./svc.sh was generated to manage your service

### Status
```bash
$ sudo ./svc.sh status

status vsts.agent.bryanmac.testsvc:

/Users/bryanmac/Library/LaunchAgents/vsts.agent.bryanmac.testsvc.plist

Started:
25324 0 vsts.agent.bryanmac.testsvc
```

Left number is the pid if the service is running

### Stop
```bash
$ sudo ./svc.sh stop

stopping vsts.agent.bryanmac.testsvc
status vsts.agent.bryanmac.testsvc:

/Users/bryanmac/Library/LaunchAgents/vsts.agent.bryanmac.testsvc.plist

Stopped
```

### Start
```bash
$ sudo ./svc.sh start

starting vsts.agent.bryanmac.testsvc
status vsts.agent.bryanmac.testsvc:

/Users/bryanmac/Library/LaunchAgents/vsts.agent.bryanmac.testsvc.plist

Started:
25324 0 vsts.agent.bryanmac.testsvc
```

### Uninstall
```bash
$ sudo ./svc.sh uninstall

```

## OSX Auto Logon and Lock

On OSX the convenience default is to create the service as a LaunchAgent.  A LaunchAgent runs when the user logs which gives it access to the UI for UI tests.  If you want it start when the box reboots, you can configure it to auto logon that account and lock on startup.

[Auto Logon and Lock](http://www.tuaw.com/2011/03/07/terminally-geeky-use-automatic-login-more-securely/)

## Service Files

This is a convenience that simply creates a service file for you can sets permissions.  You are free to manually configure and control your service using alternate methods.

Details are in .Service file in root of the agent

OSX LaunchAgent: ~/Library/LaunchAgents/vsts.agent.{accountName}.{agentName}.plist
Linux SystemD: /etc/systemd/system/vsts.agent.{accountName}.{agentName}.service

These files are created from a template located

OSX: ./bin/vsts.agent.plist.template
Linux: ./bin/vsts.agent.service.template

For example, on OSX you could use that template to run as a launch daemon if you are not needing UI tests and/or don't want to configure auto logon lock. [Details Here](https://developer.apple.com/library/mac/documentation/MacOSX/Conceptual/BPSystemStartup/Chapters/CreatingLaunchdJobs.html)






