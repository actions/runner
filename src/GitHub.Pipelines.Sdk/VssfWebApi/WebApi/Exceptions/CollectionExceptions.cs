using System;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Collection
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "CollectionDoesNotExistException", "GitHub.Services.Collection.CollectionDoesNotExistException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
