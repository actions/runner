using System;
using System.Collections.Generic;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// The body of a PATCH request to save tags for Visual Studio account resource.
    /// </summary>
    public class AccountTagRequest
    {
        /// <summary>
        /// The custom tags of the resource.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }
    }
}
