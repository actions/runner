using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Launch.Contracts
{
    [DataContract]
    public class ActionReferenceRequest
    {
        [DataMember(EmitDefaultValue = false, Name = "action")]
        public string Action { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "version")]
        public string Version { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "path")]
        public string Path { get; set; }
    }
    
    [DataContract]
    public class ActionReferenceRequestList
    {
        [DataMember(EmitDefaultValue = false, Name = "actions")]
        public IList<ActionReferenceRequest> Actions { get; set; }
    }

    [DataContract]
    public class ActionDownloadInfoResponse
    {
        [DataMember(EmitDefaultValue = false, Name = "authentication")]
        public ActionDownloadAuthenticationResponse Authentication { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "name")]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "resolved_name")]
        public string ResolvedName { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "resolved_sha")]
        public string ResolvedSha { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "tar_url")]
        public string TarUrl { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "version")]
        public string Version { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "zip_url")]
        public string ZipUrl { get; set; }
    }

    [DataContract]
    public class ActionDownloadAuthenticationResponse
    {
        [DataMember(EmitDefaultValue = false, Name = "expires_at")]
        public DateTime ExpiresAt { get; set; }

        [DataMember(EmitDefaultValue = false, Name = "token")]
        public string Token { get; set; }
    }

    [DataContract]
    public class ActionDownloadInfoResponseCollection
    {
        /// <summary>A mapping of action specifications to their download information.</summary>
        /// <remarks>The key is the full name of the action plus version, e.g. "actions/checkout@v2".</remarks>
        [DataMember(EmitDefaultValue = false, Name = "actions")]
        public IDictionary<string, ActionDownloadInfoResponse> Actions { get; set; }
    }


}