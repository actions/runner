using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Notification
{
    [DataContract]
    public class Notification
    {
        [DataMember]
        public Int64 Id { get; set; }
        [DataMember]
        public Guid Recipient { get; set; }
        [DataMember]
        public string Scope { get; set; }
        [DataMember]
        public string Content { get; set; }
        [DataMember]
        public string Category { get; set; }
        [DataMember]
        public DateTime CreatedTime { get; set; }
        [DataMember]
        public string ActionUrl { get; set; }

        public Notification()
        {
        }

        public Notification(Guid recipient, string scope, string content, string category, DateTime createdTime, string actionUrl)
            :this(-1, recipient, scope, content, category, createdTime, actionUrl)
        {
        }

        public Notification(Int64 id, Guid recipient, string scope, string content, string category, DateTime createdTime, string actionUrl)
        {
            this.Id = id;
            this.Recipient = recipient;
            this.Scope = scope;
            this.Content = content;
            this.Category = category;
            this.CreatedTime = createdTime;
            this.ActionUrl = actionUrl;
        }
    }
}
