# Container Hooks
This repo contains example implementation of the container hook feature across various container providers. More information on how to implement your own hooks can be found in the [github docs]().

Three projects are included in the `src` folder
- k8s: A kubernetes hook implementation that spins up pods dynamically to run a job
- docker: A hook implementation of the runner's docker implementation 
- hooklib: a shared library which contains typescript definitions and utilities that the other projects consume

### Want to contribute
We welcome contributions.  See [how to contribute](CONTRIBUTING.md).