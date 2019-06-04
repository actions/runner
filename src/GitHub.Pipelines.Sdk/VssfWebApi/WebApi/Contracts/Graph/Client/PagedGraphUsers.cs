using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Graph.Client
{
    [DataContract]
    public class PagedGraphUsers
    {
        /// <summary>
        /// This will be non-null if there is another page of data. There will never be more than
        /// one continuation token returned by a request.
        /// </summary>
        [ClientResponseHeader(Common.Internal.HttpHeaders.MsContinuationToken)]
        public IEnumerable<string> ContinuationToken { get; set; }

        /// <summary>
        /// The enumerable set of users found within a page.
        /// </summary>
        [ClientResponseContent]
        public IEnumerable<GraphUser> GraphUsers { get; set; }
    }
}
