using System;
using System.Collections.Generic;

namespace GitHub.Services.WebApi
{
    public interface IPagedList<T> : IList<T>
    {
        string ContinuationToken { get; }
    }

    public class PagedList<T> : List<T>, IPagedList<T>
    {
        public PagedList(IEnumerable<T> list, String continuationToken)
            : base(list)
        {
            this.ContinuationToken = continuationToken;
        }

        public String ContinuationToken
        {
            get;
            private set;
        }
    }
}
