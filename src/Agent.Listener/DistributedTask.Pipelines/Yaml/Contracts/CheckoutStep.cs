using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.TypeConverters;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts
{
    internal sealed class CheckoutStep : ISimpleStep
    {
        public String Name { get; set; }

        internal String Clean { get; set; }

        internal String FetchDepth { get; set; }

        internal String Lfs { get; set; }

        public ISimpleStep Clone()
        {
            return new CheckoutStep
            {
                Name = Name,
                Clean = Clean,
                FetchDepth = FetchDepth,
                Lfs = Lfs,
            };
        }

        internal IList<Variable> GetVariables(IList<ProcessResource> resources)
        {
            var variables = new List<Variable>();
            switch (Name ?? String.Empty)
            {
                case YamlConstants.None:
                    variables.Add(new Variable() { Name = "agent.source.skip", Value = "true" });
                    break;

                case YamlConstants.Self:
                    ProcessResource repo = null;
                    if (resources != null)
                    {
                        repo = resources.FirstOrDefault((ProcessResource resource) =>
                        {
                            return String.Equals(resource.Type, YamlConstants.Repo, StringComparison.OrdinalIgnoreCase) &&
                                String.Equals(resource.Name, Name, StringComparison.OrdinalIgnoreCase);
                        });
                    }

                    String clean = !String.IsNullOrEmpty(Clean) ? Clean : TryGetDataValue(repo, YamlConstants.Clean);
                    if (!String.IsNullOrEmpty(clean))
                    {
                        variables.Add(new Variable() { Name = "build.repository.clean", Value = clean });
                    }

                    String fetchDepth = !String.IsNullOrEmpty(FetchDepth) ? FetchDepth : TryGetDataValue(repo, YamlConstants.FetchDepth);
                    if (!String.IsNullOrEmpty(fetchDepth))
                    {
                        variables.Add(new Variable() { Name = "agent.source.git.shallowFetchDepth", Value = fetchDepth });
                    }

                    String lfs = !String.IsNullOrEmpty(Lfs) ? Lfs : TryGetDataValue(repo, YamlConstants.Lfs);
                    if (!String.IsNullOrEmpty(lfs))
                    {
                        variables.Add(new Variable() { Name = "agent.source.git.lfs", Value = lfs });
                    }

                    break;

                default:
                    // Should not reach here.
                    throw new NotSupportedException($"Unexpected checkout step resource name: '{Name}'");
            }

            return variables;
        }

        private static String TryGetDataValue(ProcessResource repo, String key)
        {
            Object obj;
            if (repo != null && repo.Data.TryGetValue(key, out obj))
            {
                return obj as String;
            }

            return null;
        }
    }
}
