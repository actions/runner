using System;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public class MsdnEntitlement : ICloneable
    {
        /// <summary>
        /// Entilement id assigned to Entitlement in Benefits Database.
        /// </summary>
        public string EntitlementCode { get; set; }

        /// <summary>
        /// Entitlement Name e.g. Downloads, Chat.
        /// </summary>
        public string EntitlementName { get; set; }

        /// <summary>
        /// Type of Entitlement e.g. Downloads, Chat.
        /// </summary>
        public string EntitlementType { get; set; }

        /// <summary>
        /// Entitlement availability
        /// </summary>
        public bool IsEntitlementAvailable { get; set; }

        /// <summary>
        /// Entitlement activation status
        /// </summary>
        public bool? IsActivated { get; set; }

        /// <summary>
        /// Subscription Expiration Date.
        /// </summary>
        public DateTimeOffset SubscriptionExpirationDate { get; set; }

        /// <summary>
        /// Subscription id which identifies the subscription itself. This is the Benefit Detail Guid from BMS.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Identifier of the subscription or benefit level.
        /// </summary>
        public string SubscriptionLevelCode { get; set; }

        /// <summary>
        /// Name of subscription level.
        /// </summary>
        public string SubscriptionLevelName { get; set; }

        /// <summary>
        /// Subscription Status Code (ACT, PND, INA ...).
        /// </summary>
        public string SubscriptionStatus { get; set; }

        /// <summary>
        /// Write MSDN Channel into CRCT (Retail,MPN,VL,BizSpark,DreamSpark,MCT,FTE,Technet,WebsiteSpark,Other)
        /// </summary>
        public string SubscriptionChannel { get; set; }

        /// <summary>
        /// Overloading ToString for objects of this class 
        /// </summary>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(string.Format("[EntitlementCode: {0}; ", this.EntitlementCode));
            sb.Append(string.Format("EntitlementType: {0}; ", this.EntitlementType));
            sb.Append(string.Format("EntitlementName: {0}; ", this.EntitlementName));
            sb.Append(string.Format("IsEntitlementAvailable: {0}; ", this.IsEntitlementAvailable));
            sb.Append(string.Format("SubscriptionExpirationDate: {0}; ", this.SubscriptionExpirationDate.ToString()));
            sb.Append(string.Format("SubscriptionId: {0}; ", this.SubscriptionId));
            sb.Append(string.Format("SubscriptionLevelCode: {0}; ", this.SubscriptionLevelCode));
            sb.Append(string.Format("SubscriptionLevelName: {0}; ", this.SubscriptionLevelName));
            sb.Append(string.Format("SubscriptionStatus: {0}; ", this.SubscriptionStatus));
            sb.Append(string.Format("SubscriptionChannel: {0}] ", this.SubscriptionChannel));
            return sb.ToString();
        }

        public MsdnEntitlement Clone()
        {
            return (MsdnEntitlement)this.MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return this.MemberwiseClone();
        }
    }
}