using System;
using System.Collections.Generic;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// The body of a PUT request to modify a Visual Studio account resource.
    /// </summary>
    public class AccountResourceRequest
    {
        /// <summary>
        /// The Azure instance location.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The custom tags of the resource.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// The custom properties of the resource.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// The type of the operation.
        /// </summary>
        public AccountResourceRequestOperationType OperationType { get; set; }

        /// <summary>
        /// The UPN.
        /// </summary>
        public string Upn { get; set; }

        /// <summary>
        /// The account name.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Source of the request.
        /// </summary>
        public string RequestSource { get; set; }
    }
}
