namespace GitHub.Services.Directories
{
    /// <summary>
    /// All directory groups implement this read-only interface.
    /// </summary>
    public interface IDirectoryGroup : IDirectoryEntity
    {
        /// <summary>
        /// Returns this group's description.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["Description"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string Description { get; }

        /// <summary>
        /// Returns this group's mail address.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["Mail"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string Mail { get; }

        /// <summary>
        /// Returns this group's mail nickname.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["MailNickname"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string MailNickname { get; }
    }
}
