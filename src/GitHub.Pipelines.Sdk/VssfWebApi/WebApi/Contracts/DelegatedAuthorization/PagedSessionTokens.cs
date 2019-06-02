using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class PagedSessionTokens
    {
        public int NextRowNumber { get; set; }

        public IList<SessionToken> SessionTokens { get; set; }
    }
}
