namespace Microsoft.VisualStudio.Services.Directories
{
    /// <summary>
    /// All directory users implement this read-only interface.
    /// </summary>
    public interface IDirectoryUser : IDirectoryEntity
    {
        /// <summary>
        /// Returns this user's department.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["Department"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string Department { get; }

        /// <summary>
        /// Returns this user's guest status.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["Guest"] as bool?</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        bool? Guest { get; }

        /// <summary>
        /// Returns this user's job title.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["JobTitle"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string JobTitle { get; }

        /// <summary>
        /// Returns this user's mail address.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["Mail"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string Mail { get; }

        /// <summary>
        /// Returns this user's mail nickname.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["MailNickname"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string MailNickname { get; }

        /// <summary>
        /// Returns this user's physical delivery office name.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["PhysicalDeliveryOfficeName"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string PhysicalDeliveryOfficeName { get; }

        /// <summary>
        /// Returns this user's sign-in address.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["SignInAddress"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string SignInAddress { get; }

        /// <summary>
        /// Returns this user's surname.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <code>this["Surname"] as string</code>.
        /// This property may or may not be set depending on the query that was used to produce this object.
        /// </remarks>
        string Surname { get; }
    }
}
