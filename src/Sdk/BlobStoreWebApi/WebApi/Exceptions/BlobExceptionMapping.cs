﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using GitHub.Services.BlobStore.WebApi.Exceptions;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    /// BlobExceptionMapping.
    /// </summary>
    /// <remarks>
    /// DEVNOTE, Bug: Beware developer, if you're adding exceptions that are considered shared i.e., they are passed along
    /// across different components/services, make it so that they are part of the common libraries and also do ensure that the
    /// mapping for it is added in the respective controller mapping too.
    /// Otherwise, the mapping translation won't occur as expected and your exception(s),
    /// regardless of whether you want them to be retry-able or not, will get retried, numerous times.
    /// </remarks>
    public static class BlobExceptionMapping
    {
        private static Lazy<Dictionary<string, Type>> translation = new Lazy<Dictionary<string, Type>>(InitClientTranslatedExceptions);
        private static Lazy<Dictionary<Type, HttpStatusCode>> errorMap = new Lazy<Dictionary<Type, HttpStatusCode>>(InitServerErrorMap);

        public static Dictionary<string, Type> ClientTranslatedExceptions => translation.Value;

        public static Dictionary<Type, HttpStatusCode> ServerErrorMap => errorMap.Value;

        private static Dictionary<string, Type> InitClientTranslatedExceptions()
        {
            var translation = new Dictionary<String, Type>
            {
                {"BlobNotFoundException", typeof(BlobNotFoundException)},
                {"InvalidCastException", typeof(InvalidCastException)},
                {"SerializationException", typeof(SerializationException)},
                {"ClientToolNotFoundException", typeof(ClientToolNotFoundException)},
                {"ArtifactBillingException", typeof(ArtifactBillingException)},
            };

            return translation;
        }

        private static Dictionary<Type, HttpStatusCode> InitServerErrorMap()
        {
            var errorMapping = new Dictionary<Type, HttpStatusCode>
            {
                {typeof(BlobNotFoundException), HttpStatusCode.NotFound},
                {typeof(ArgumentException), HttpStatusCode.BadRequest},
                {typeof(InvalidCastException), HttpStatusCode.BadRequest},
                {typeof(SerializationException), HttpStatusCode.BadRequest},
                {typeof(ClientToolNotFoundException), HttpStatusCode.NotFound},
                {typeof(ArtifactBillingException), HttpStatusCode.PaymentRequired},
            };

            return errorMapping;
        }
    }
}
