using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Directories
{
    public static class DirectoryEntityState
    {
        /// <summary>
        /// Returned when an entity has been retrieved from VSTS.
        /// </summary>
        public const string LocalFetch = "LocalFetch";

        /// <summary>
        /// Returned when an entity has been retrieved from outside VSTS and created in VSTS.
        /// </summary>
        public const string LocalCreate = "LocalCreate";

        /// <summary>
        /// Returned when an entity has been retrieved from outside VSTS without being created in VSTS.
        /// </summary>
        public const string RemoteFetch = "RemoteFetch";
    }
}
