using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Encapsulates azure specific plan structure, using a publisher defined publisher name, offer name, and plan name
    /// These are all specified by the publisher and can vary from other meta data we store about the extension internally
    /// therefore need to be tracked seperately for purposes of interacting with Azure
    /// </summary>
    public class AzureOfferPlanDefinition : IEquatable<AzureOfferPlanDefinition>
    {
        /// <summary>
        /// The meter id which identifies the offer meter this plan is associated with
        /// </summary>
        public int MeterId { get; set; }

        /// <summary>
        /// The id of the plan, which is usually in the format "{publisher}:{offer}:{plan}"
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// The publisher of the plan as defined by the publisher in Azure
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// The offer / product name as defined by the publisher in Azure
        /// </summary>
        public string OfferName { get; set; }

        /// <summary>
        /// The offer / product name as defined by the publisher in Azure
        /// </summary>
        public string OfferId { get; set; }

        /// <summary>
        /// The plan name as defined by the publisher in Azure
        /// </summary>
        public string PlanName { get; set; }

        /// <summary>
        /// The version string which optionally identifies the version of the plan
        /// </summary>
        public string PlanVersion { get; set; }

        /// <summary>
        /// The number of users associated with the plan as defined in Azure
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Determines whether or not this plan is visible to all users
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// get/set publisher name
        /// </summary>
        public string PublisherName { get; set; }


        public bool Equals(AzureOfferPlanDefinition plan)
        {
            return this.Equals(plan, compareForUpdate: false);
        }

        /// <summary>
        /// Determines whether the specified <see cref="AzureOfferPlanDefinition"/> is equal to the current <see cref="AzureOfferPlanDefinition"/>.
        /// </summary>
        /// <param name="plan">The object to compare with the current object.</param>
        /// <param name="compareForUpdate">Whether to compare for the purposes of updating an existing AzureOfferPlanDefinition. If false, all properties are compared.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false</returns>
        public bool Equals(AzureOfferPlanDefinition plan, bool compareForUpdate)
        {
            if (plan != null)
            {
                if (compareForUpdate)
                {
                    return string.Equals(this.PlanId, plan.PlanId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(this.Publisher, plan.Publisher, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(this.OfferName, plan.OfferName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(this.PlanName, plan.PlanName, StringComparison.OrdinalIgnoreCase) &&
                        this.Quantity == plan.Quantity &&
                        string.Equals(this.PublisherName, plan.PublisherName, StringComparison.OrdinalIgnoreCase);
                }
                return this.MeterId == plan.MeterId &&
                       this.PlanVersion == plan.PlanVersion &&
                       this.IsPublic == plan.IsPublic &&
                       this.PlanId == plan.PlanId &&
                       this.Publisher == plan.Publisher &&
                       this.OfferName == plan.OfferName &&
                       this.PlanName == plan.PlanName &&
                       this.Quantity == plan.Quantity &&
                       this.OfferId == plan.OfferId &&
                       this.PublisherName == plan.PublisherName;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return PlanId != null ? PlanId.GetHashCode() : MeterId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as AzureOfferPlanDefinition);
        }

        public AzureOfferPlanDefinition Clone()
        {
            return new AzureOfferPlanDefinition
            {
                MeterId = this.MeterId,
                PlanId = this.PlanId,
                Publisher = this.Publisher,
                OfferName = this.OfferName,
                PlanName = this.PlanName,
                PlanVersion = this.PlanVersion,
                Quantity = this.Quantity,
                IsPublic = this.IsPublic,
                OfferId = this.OfferId,
                PublisherName = this.PublisherName
            };
        }

        public static bool operator == (AzureOfferPlanDefinition left, AzureOfferPlanDefinition right)
        {
            if ((object)left == null || (object)right == null)
                return Object.Equals(left, right);

            return left.Equals(right);
        }

        public static bool operator != (AzureOfferPlanDefinition left, AzureOfferPlanDefinition right)
        {
            return !(left == right);
        }
    }

    public class AzureOfferPlanDefinitionNameComparer : IEqualityComparer<AzureOfferPlanDefinition>
    {

        public bool Equals(AzureOfferPlanDefinition first, AzureOfferPlanDefinition second)
        {
            if (object.ReferenceEquals(first, second))
            {
                return true;
            }

            return first != null && second != null && first.PlanName.Equals(second.PlanName);
        }

        public int GetHashCode(AzureOfferPlanDefinition obj)
        {
            return obj.PlanName == null ? 0 : obj.PlanName.GetHashCode();
        }
    }

    public class AzureOfferPlanDefinitionUpdateComparer : IEqualityComparer<AzureOfferPlanDefinition>
    {
        public bool Equals(AzureOfferPlanDefinition first, AzureOfferPlanDefinition second)
        {
            if (object.ReferenceEquals(first, second))
            {
                return true;
            }

            return first != null && second != null && first.Equals(second, compareForUpdate: true);
        }

        public int GetHashCode(AzureOfferPlanDefinition obj)
        {
            int hashCode = obj.PlanId != null ? obj.PlanId.GetHashCode() : 0;
            hashCode = (hashCode * 17) ^ (obj.Publisher != null ? obj.Publisher.GetHashCode() : 0);
            hashCode = (hashCode * 17) ^ (obj.OfferName != null ? obj.OfferName.GetHashCode() : 0);
            hashCode = (hashCode * 17) ^ (obj.OfferId != null ? obj.OfferId.GetHashCode() : 0);
            hashCode = (hashCode * 17) ^ (obj.PlanName != null ? obj.PlanName.GetHashCode() : 0);
            hashCode = (hashCode * 17) ^ obj.Quantity;
            hashCode = (hashCode * 17) ^ (obj.PublisherName != null ? obj.PublisherName.GetHashCode() : 0);
            return hashCode;
        }
    }
}