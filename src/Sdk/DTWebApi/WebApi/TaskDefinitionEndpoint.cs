using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskDefinitionEndpoint
    {
        /// <summary>
        /// The scope as understood by Connected Services.
        /// Essentialy, a project-id for now.
        /// </summary>
        [DataMember]
        public String Scope
        {
            get;
            set;
        }

        /// <summary>
        /// URL to GET.
        /// </summary>
        [DataMember]
        public String Url
        {
            get;
            set;
        }

        /// <summary>
        /// An XPath/Json based selector to filter response returned by fetching
        /// the endpoint <c>Url</c>. An XPath based selector must be prefixed with
        /// the string "xpath:". A Json based selector must be prefixed with "jsonpath:".
        /// <example>
        /// The following selector defines an XPath for extracting nodes named 'ServiceName'.
        /// <code>
        /// endpoint.Selector = "xpath://ServiceName";
        /// </code>
        /// </example>
        /// </summary>
        [DataMember]
        public String Selector
        {
            get;
            set;
        }

        /// <summary>
        /// An Json based keyselector to filter response returned by fetching
        /// the endpoint <c>Url</c>.A Json based keyselector must be prefixed with "jsonpath:".
        /// KeySelector can be used to specify the filter to get the keys for the values specified with Selector.
        /// <example>
        /// The following keyselector defines an Json for extracting nodes named 'ServiceName'.
        /// <code>
        /// endpoint.KeySelector = "jsonpath://ServiceName";
        /// </code>
        /// </example>
        /// </summary>
        [DataMember]
        public String KeySelector
        {
            get;
            set;
        }

        /// <summary>
        /// An ID that identifies a service connection to be used for authenticating
        /// endpoint requests.
        /// </summary>
        [DataMember]
        public String ConnectionId
        {
            get;
            set;
        }

        /// <summary>
        /// TaskId that this endpoint belongs to.
        /// </summary>
        [DataMember]
        public String TaskId
        {
            get;
            set;
        }
    }
}
