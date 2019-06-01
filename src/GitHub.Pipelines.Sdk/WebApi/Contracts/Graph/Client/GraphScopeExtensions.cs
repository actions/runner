using System;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    public static class GraphScopeExtensions
    {
        public static Guid GetScopeId(this GraphScope graphScope)
        {
            Guid scopeId;
            if (!Guid.TryParse(graphScope.OriginId, out scopeId))
            {
                scopeId = Guid.Empty;
            }

            return scopeId;
        }
    }
}
