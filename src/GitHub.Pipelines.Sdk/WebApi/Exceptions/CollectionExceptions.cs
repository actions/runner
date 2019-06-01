using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Collection
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "CollectionDoesNotExistException", "Microsoft.VisualStudio.Services.Collection.CollectionDoesNotExistException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CollectionDoesNotExistException : VssServiceException
    {
        public CollectionDoesNotExistException(String collectionName)
            : base(WebApiResources.CollectionDoesNotExistException(collectionName))
        {
        }

        public CollectionDoesNotExistException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
