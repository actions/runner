using System;
using System.IO;
using GitHub.Services.BlobStore.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    /// Helper class to capture the mapping of blob to a URI where it is or should be stored
    /// </summary>
    public class BlobToUriMapping
    {
        /// <summary>
        /// A description of the content represented by this mapping.  Mostly used for error reporting.
        /// </summary>
        public string ContentSpec { get; set; }

        /// <summary>
        /// A factory that returns a stream over the represented content
        /// </summary>
        public Lazy<Stream> StreamFactory { get; set; }

        /// <summary>
        /// The URI to which the content represented here should be put.
        /// </summary>
        public string UriSpec { get; set; }

        /// <summary>
        /// The globally unique identity of the content associated with this item
        /// </summary>
        public BlobIdentifier BlobId { get; set; }

        /// <summary>
        /// Number of bytes to use from the stream. Negative one means go until EOF.
        /// </summary>
        public int BytesToCopyFromStream { get; set; }
    }
}
