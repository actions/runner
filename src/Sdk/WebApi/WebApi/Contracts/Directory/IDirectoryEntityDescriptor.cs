namespace GitHub.Services.Directories
{
    /// <summary>
    /// This a view that describes an entity which may or may not exist in a backing directory.
    /// For the interface that gives a partial view of a known directory entity, see <see cref="IDirectoryEntity"/>.
    /// </summary>
    public interface IDirectoryEntityDescriptor
    {
        /// <summary>
        /// This is the type of the target directory entity.
        /// Must be a <see cref="DirectoryEntityType"/>.
        /// </summary>
        string EntityType { get; }

        /// <summary>
        /// This is the source directory of entity. For an MSA backed org, this could
        /// be MSA or AAD, depending on which verson of identity it represents.
        /// Must be a <see cref="DirectoryName"/>.
        /// </summary>
        string EntityOrigin { get; }

        /// <summary>
        /// This is the directory that originated the target directory entity.
        /// Must be a <see cref="DirectoryName"/>.
        /// </summary>
        string OriginDirectory { get; }

        /// <summary>
        /// This is the origin directory's identifier for the target directory entity.
        /// </summary>
        string OriginId { get; }

        /// <summary>
        /// This is the directory that stores VSTS's view of the target directory entity.
        /// Must be a <see cref="DirectoryName"/>.
        /// </summary>
        string LocalDirectory { get; }

        /// <summary>
        /// This is the local directory's identifier for the target directory entity.
        /// </summary>
        string LocalId { get; }

        /// <summary>
        /// This is the principal name of the target directory entity.
        /// </summary>
        string PrincipalName { get; }

        /// <summary>
        /// This is the display name of the target directory entity.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// This gives additional property name value pairs for the target directory name.
        /// Returns null for unknown/unset properties.
        /// </summary>
        object this[string propertyName] { get; }
    }
}
