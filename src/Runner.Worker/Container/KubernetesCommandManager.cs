using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{
    public class KubernetesCommandManager : RunnerService, IContainerCommandManager
    {
        public string Type { get { return "kubernetes"; } }
        public string DockerPath { get; private set; }

        public string DockerInstanceLabel { get; private set; }

        public Task<DockerVersion> DockerVersion(IExecutionContext context)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerPull(IExecutionContext context, string image)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerPull(IExecutionContext context, string image, string configFileDirectory)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag)
        {
           throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<string> DockerCreate(IExecutionContext context, ContainerInfo container)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerStart(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerRemove(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerLogs(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<List<string>> DockerPS(IExecutionContext context, string options)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerNetworkCreate(IExecutionContext context, string network)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerNetworkRemove(IExecutionContext context, string network)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerNetworkPrune(IExecutionContext context)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> output)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<List<string>> DockerInspect(IExecutionContext context, string dockerObject, string options)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<List<PortMapping>> DockerPort(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }

        public Task<int> DockerLogin(IExecutionContext context, string configFileDirectory, string registry, string username, string password)
        {
            throw new NotImplementedException("Kubernetes support to be implemented");
        }
    }
}
