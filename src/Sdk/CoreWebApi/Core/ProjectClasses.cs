using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.Common.Internal;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Patch;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Core.WebApi
{
    [GenerateAllConstants]
    public enum ProjectState
    {
        /// <summary>
        /// Project is in the process of being deleted.
        /// </summary>
        [EnumMember]
        Deleting = 2,

        /// <summary>
        /// Project is in the process of being created.
        /// </summary>
        [EnumMember]
        New = 0,

        /// <summary>
        /// Project is completely created and ready to use.
        /// </summary>
        [EnumMember]
        WellFormed = 1,

        /// <summary>
        /// Project has been queued for creation, but the process has not yet started.
        /// </summary>
        [EnumMember]
        CreatePending = 3,

        /// <summary>
        /// All projects regardless of state.
        /// </summary>
        [EnumMember]
        All = -1, // Used for filtering.

        /// <summary>
        /// Project has not been changed.
        /// </summary>
        [EnumMember]
        Unchanged = -2, // Used for updating projects.

        /// <summary>
        /// Project has been deleted.
        /// </summary>
        [EnumMember]
        Deleted = 4, // Used for the project history.
    }

    public enum ProjectVisibility // Stored as a TINYINT
    {
        [ClientInternalUseOnly]
        Unchanged = -1, // Used for updating projects.
        /// <summary>
        /// The project is only visible to users with explicit access.
        /// </summary>
        Private = 0,
        /// <summary>
        /// Enterprise level project visibility
        /// </summary>
        [ClientInternalUseOnly(omitFromTypeScriptDeclareFile: false)]
        Organization = 1,
        /// <summary>
        /// The project is visible to all.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Public = 2,
        [ClientInternalUseOnly]
        SystemPrivate = 3  // Soft-deleted projects
    }
}
