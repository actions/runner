using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class SvnSourceProvider : SourceProvider, ISourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.Svn;
    }
}