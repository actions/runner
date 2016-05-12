# Running As A Service On Unix and OSX

## Using Your Path

If you install dev tools and have customized your $PATH, you can snapshot it for the service to use.

```bash
echo $PATH > .Path
```

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
