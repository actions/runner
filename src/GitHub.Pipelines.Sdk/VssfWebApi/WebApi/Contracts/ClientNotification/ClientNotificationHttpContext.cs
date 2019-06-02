using System.ComponentModel;

namespace Microsoft.VisualStudio.Services.ClientNotification
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ClientNotificationHttpContext
    {
        /// <summary>
        /// 
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public NotificationType NotificationType { get; set; }
    }
}
