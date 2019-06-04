using GitHub.Services.Common;

namespace GitHub.Services.Directories
{
    /// <summary>
    /// <para>
    /// This is a partial view of a known directory entity at a point in time.
    /// Two views of the same logical directory entity may contain different properties.
    /// </para>
    /// <para>
    /// All directory entities implement this read-only interface.
    /// </para>
    /// </summary>
    public interface IDirectoryEntity : IDirectoryEntityDescriptor
    {
        /// <summary>
        /// <para>
        /// This is an opaque identifier for this directory entity.
        /// </para>
        /// <para>
        /// If two identifiers are equal, then they reference the same logical directory entity.
        /// If entity 1 has identifier "x" and entity 2 has identifier "x", then the entitys are views of the same directory entity.
        /// </para>
        /// <para>
        /// If two identifiers are not equal, then they may reference different logical directory entitys or the same logical directory entity.
        /// In most cases, if entity 1 has identifier "x" and entity 2 has identifier "y", then the entitys are views of different directory entitys.
        /// However, in some cases, such as when the format of the identifier changes, then the entitys are views of the same directory entity.
        /// </para>
        /// <para>
        /// The format of this identifier is subject to change without notice and clients should not attempt to parse it.
        /// </para>
        /// </summary>
        string EntityId { get; }

        /// <summary>
        /// This is the type of this directory entity.
        /// Must be a concrete <see cref="DirectoryEntityType"/>.
        /// If <see cref="DirectoryEntityType.User"/>, implies that this entity is an <see cref="IDirectoryUser"/>.
        /// If <see cref="DirectoryEntityType.Group"/>, implies that this entity is an <see cref="IDirectoryGroup"/>.
        /// This property should not be null.
        /// </summary>
        new string EntityType { get; }

        /// <summary>
        /// This is the origin directory of entity. For an MSA backed org, this could
        /// be MSA or AAD, depending on which verson of identity it represents.
        /// Must be a concrete <see cref="DirectoryName"/>.
        /// </summary>
        new string EntityOrigin { get; }

        /// <summary>
        /// This is the directory that originated this directory entity.
        /// Must be a concrete <see cref="DirectoryName"/>.
        /// This property should not be null.
        /// </summary>
        new string OriginDirectory { get; }

        /// <summary>
        /// This is the origin directory's identifier for this directory entity.
        /// This property should not be null.
        /// </summary>
        new string OriginId { get; }

        /// <summary>
        /// This is the directory that stores VSTS's view of this directory entity.
        /// Must be a concrete <see cref="DirectoryName"/>.
        /// This property should not be null.
        /// </summary>
        new string LocalDirectory { get; }

        /// <summary>
        /// This is the local directory's identifier for this directory entity.
        /// This property may be null depending on whether the entity has been added to VSTS.
        /// </summary>
        new string LocalId { get; }

        /// <summary>
        /// Returns this entity's principal name.
        /// This property may or may not be set depending on the query that was used to produce this entity.
        /// </summary>
        new string PrincipalName { get; }

        /// <summary>
        /// Returns this entity's display name.
        /// This property may or may not be set depending on the query that was used to produce this entity.
        /// </summary>        
        new string DisplayName { get; }

        /// <summary>
        /// Returns this entity's scope name.
        /// <para>For an entity that originates from VSD, the scope name is the account name.</para>
        /// <para>For an entity that originates from AAD, the scope name is the tenant name.</para>
        /// <para>This property may or may not be set depending on the query that was used to produce this entity.</para>
        /// </summary>
        string ScopeName { get; }

        /// <summary>
        /// Returns this entity's local identity descriptor which gives its identity type and security identifier.
        /// <para>This property may or may not be set depending on the query that was used to produce this entity.</para>
        /// </summary>
        string LocalDescriptor { get; }

        /// <summary>
        /// Returns this entity's identity subject descriptor which gives its subject type and identifier.
        /// <para>This property may or may not be set depending on the query that was used to produce this entity.</para>
        /// </summary>
        SubjectDescriptor? SubjectDescriptor { get; }

        /// <summary>
        /// Returns this entity's active status in the directory.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["Active"] as bool?</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        bool? Active { get; }
    }
}
