using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Notification
{
    /// <summary>
    /// Notification-related metadata for a specific user
    /// </summary>
    [DataContract]
    public class RecipientMetadata
    {
        [DataMember]
        public Guid RecipientId { get; set; }

        [DataMember]
        public Int64 IdOfMostRecentNotification { get; set; }

        [DataMember]
        public Int64 IdOfMostRecentSeenNotification { get; set; }

        [DataMember]
        public int NumberOfUnseenNotifications { get; set; }

        public RecipientMetadata()
        {
        }

        public RecipientMetadata(Guid recipientId, Int64 highestUnseenNotificationId, int unseenNotificationCount)
        {
            this.RecipientId = recipientId;
            this.IdOfMostRecentNotification = highestUnseenNotificationId;
            this.NumberOfUnseenNotifications = unseenNotificationCount;
        }        
    }
}
