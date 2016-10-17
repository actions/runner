namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class DeploymentJobExtension : ReleaseJobExtension
    {
        public override string HostType => "deployment";
    }
}