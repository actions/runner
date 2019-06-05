using System;
using System.ComponentModel;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Any responses from public APIs must implement this interface. It is used to enforce that 
    /// the data being returned has been security checked.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISecuredObject
    {
        /// <summary>
        /// The id of the namespace which secures this resource.
        /// </summary>
        Guid NamespaceId
        {
            get;
        }

        /// <summary>
        /// The security bit to demand.
        /// </summary>
        Int32 RequiredPermissions
        {
            get;
        }

        /// <summary>
        /// The token to secure this resource.
        /// </summary>
        String GetToken();
    }

    /// <summary>
    /// Containers of ISecuredObjects should implement this interface. If you implement this interface, all
    /// serializable properties must be of type ISecuredObject or IEnumerable of ISecuredObject. This will
    /// be enforced using a roslyn analyzer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISecuredObjectContainer { }
}
