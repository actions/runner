using System;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPackageStore
    {
        PackageVersion GetLatestVersion(String packageType);
    }
}
