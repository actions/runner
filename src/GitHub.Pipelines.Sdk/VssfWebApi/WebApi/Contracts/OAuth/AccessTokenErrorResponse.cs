﻿using System;
using System.Runtime.Serialization;

namespace GitHub.Services.OAuth
{
    [DataContract]
    public sealed class AccessTokenErrorResponse
    {
        public AccessTokenErrorResponse()
        {
        }

        public AccessTokenErrorResponse(
            String error)
        {
            Error = error;
        }

        [DataMember(Name = "error")]
        public String Error { get; set; }
    }
}
