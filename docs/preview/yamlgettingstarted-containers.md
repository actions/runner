# YAML getting started - Containers

When using an agent pool, a phase can specify a `container` to use. The agent will start
an instance of the container, and will run the steps inside the container.

For example:

```yaml
resources:
  containers:
  - container: dev
    image: ubuntu:16.04
queue:
  container: dev
steps:
- script: printenv
```

## With a matrix strategy

A matrix can be used to run a phase multiple times, using different containers.
The variables defined by the matrix can be used to specify the container resource.

For example:

```yaml
resources:
  containers:
  - container: u14
    image: ubuntu:14.04

  - container: u14_private
    image: private:ubuntu14
    endpoint: privatedockerhub

  - container: u16
    image: ubuntu:16.04
    options: --cpu-count 4

  - container: u16_local
    image: ubuntu:16.04
    options: --hostname container-test --ip 192.168.0.1
    localImage: true
    env:
      envVariable1: envValue1
      envVariable2: envValue2

queue:
  matrix:
    ubuntu14:
      containerResource: u14
    ubuntu14_private:
      containerResource: u14_private
    ubuntu16:
      containerResource: u16
    ubuntu16_local:
      containerResource: u16_local
  container: $[ variables['containerResource'] ]

steps:
  - script: printenv
```
