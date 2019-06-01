using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPackageStore
    {
        PackageVersion GetLatestVersion(String packageType);
    }
}
