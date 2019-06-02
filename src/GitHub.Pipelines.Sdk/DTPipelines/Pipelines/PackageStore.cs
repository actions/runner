using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    public interface IPackageResolver
    {
        IList<PackageMetadata> GetPackages(String packageType);
    }

    public class PackageStore : IPackageStore
    {
        public PackageStore(params PackageMetadata[] packages)
            : this(packages, null)
        {
        }

        public PackageStore(
            IEnumerable<PackageMetadata> packages = null, 
            IPackageResolver resolver = null)
        {
            this.Resolver = resolver;
            m_packages = packages?.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase) ?? 
                new Dictionary<String, List<PackageMetadata>>(StringComparer.OrdinalIgnoreCase);
        }

        public IPackageResolver Resolver
        {
            get;
        }

        public PackageVersion GetLatestVersion(String packageType)
        {
            if (!m_packages.TryGetValue(packageType, out var existingPackages))
            {
                var resolvedPackages = this.Resolver?.GetPackages(packageType);
                if (resolvedPackages?.Count > 0)
                {
                    existingPackages = resolvedPackages.ToList();
                    m_packages[packageType] = existingPackages;
                }
            }

            return existingPackages?.OrderByDescending(x => x.Version).Select(x => x.Version).FirstOrDefault();
        }

        private Dictionary<String, List<PackageMetadata>> m_packages;
    }
}
