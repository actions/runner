using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Core.WebApi
{
    /// <summary>
    /// Represents a shallow reference to a TeamProject.
    /// </summary>
    [DataContract]
    public class TeamProjectReference : ISecuredObject
    {
        /// <summary>
        /// Default constructor to ensure we set up the project state correctly for serialization.
        /// </summary>
        public TeamProjectReference()
        {
            State = ProjectState.Unchanged;
            Visibility = ProjectVisibility.Unchanged;
        }

        /// <summary>
        /// Project identifier.
        /// </summary>
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Project abbreviation.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string Abbreviation { get; set; }

        /// <summary>
        /// Project name.
        /// </summary>
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// The project's description (if any).
        /// </summary>
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Url to the full version of the object.
        /// </summary>
        [DataMember(Order = 4, EmitDefaultValue = false)]
        public string Url { get; set; }

        /// <summary>
        /// Project state.
        /// </summary>
        [DataMember(Order = 5)]
        public ProjectState State { get; set; }

        /// <summary>
        /// Project revision.
        /// </summary>
        [DataMember(Order = 6, EmitDefaultValue = false)]
        public Int64 Revision { get; set; }

        /// <summary>
        /// Project visibility.
        /// </summary>
        [DataMember(Order = 7)]
        public ProjectVisibility Visibility { get; set; }

        /// <summary>
        /// Url to default team identity image.
        /// </summary>
        [DataMember(Order = 8, EmitDefaultValue = false)]
        public String DefaultTeamImageUrl { get; set; }

        /// <summary>
        /// Project last update time.
        /// </summary>
        [DataMember(Order = 9)]
        public DateTime LastUpdateTime { get; set; }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => NamespaceId;

        int ISecuredObject.RequiredPermissions => RequiredPermissions;

        string ISecuredObject.GetToken()
        {
            return GetToken();
        }

        protected virtual Guid NamespaceId => TeamProjectSecurityConstants.NamespaceId;

        protected virtual int RequiredPermissions => TeamProjectSecurityConstants.GenericRead;

        protected virtual string GetToken()
        {
            // WE DON'T CARE THIS FOR NOW
            return TeamProjectSecurityConstants.GetToken(Id.ToString("D"));
        }

        #endregion
    }
}
