using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Commerce
{
    [DebuggerDisplay("{MeterId} | {Name} | {GalleryId}")]
    public class OfferMeter : IOfferMeter, IEquatable<OfferMeter>
    {
        /// <summary>
        /// Meter Id.
        /// </summary>
        public int MeterId { get; set; }

        /// <summary>
        /// Gets or sets the identifier representing this meter in commerce platform
        /// </summary>
        public Guid PlatformMeterId { get; set; }

        /// <summary>
        /// Gets or sets Gallery Id.
        /// </summary>
        public string GalleryId { get; set; }

        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Renewak Frequency.
        /// </summary>
        public MeterRenewalFrequecy RenewalFrequency { get; set; }

        /// <summary>
        /// Gets or sets the billing mode of the resource
        /// </summary>
        public ResourceBillingMode BillingMode { get; set; }

        /// <summary>
        /// Category.
        /// </summary>
        public MeterCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the offer scope.
        /// </summary>
        public OfferScope OfferScope { get; set; }

        /// <summary>
        /// Gets or sets the state of the billing.
        /// </summary>
        public MeterBillingState BillingState { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public MeterState Status { get; set; }

        /// <summary>
        /// Measuring unit for this meter.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets the user assignment model.
        /// </summary>
        public OfferMeterAssignmentModel AssignmentModel { get; set; }

        /// <summary>
        /// Gets or sets the billing start date.
        /// If TrialDays + PreviewGraceDays > then, on 'BillingStartDate' it starts the preview Grace and/or trial period.
        /// </summary>
        public DateTime? BillingStartDate { get; set; }

        /// <summary>
        /// Gets or sets the trial days.
        /// </summary>
        public byte TrialDays { get; set; }

        /// <summary>
        /// Gets or sets the preview grace days.
        /// </summary>
        public byte PreviewGraceDays { get; set; }

        /// <summary>
        /// Quantity included for free.
        /// </summary>
        public Int32 IncludedQuantity { get; set; }

        /// <summary>
        /// Quantity used by the user, when resources is pay as you go or commitment based.
        /// </summary>
        public Int32 CurrentQuantity { get; set; }

        /// <summary>
        /// Quantity commited by the user, when resources is commitment based.
        /// </summary>
        public Int32 CommittedQuantity { get; set; }

        /// <summary>
        /// Gets or sets the value of maximum quantity for the resource
        /// </summary>
        public int MaximumQuantity { get; set; }

        /// <summary>
        /// Gets or sets the value of absolute maximum quantity for the resource
        /// </summary>
        public int AbsoluteMaximumQuantity { get; set; }

        /// <summary>
        /// Gets or sets the trial cycles.
        /// </summary>
        public int TrialCycles { get; set; }

        /// <summary>
        /// Indicates whether users get auto assigned this license type duing first access.
        /// </summary>
        public bool AutoAssignOnAccess { get; set; }

        /// <summary>
        /// Gets or sets the responsible entity/method for billing. Determines how this meter is handled in the backend.
        /// </summary>
        public BillingProvider BillingEntity { get; set; }

        /// <summary>
        /// Gets or sets the minimum required access level for the meter.
        /// </summary>
        public MinimumRequiredServiceLevel MinimumRequiredAccessLevel { get; set; }

        /// <summary>
        /// Gets or sets the Min license level the offer is free for.
        /// </summary>
        public MinimumRequiredServiceLevel IncludedInLicenseLevel { get; set; }

        /// <summary>
        /// Gets or sets the map of named quantity varied plans, plans can be purchased that vary only in the number of users included.
        /// Null if this offer meter does not support named fixed quantity plans.
        /// </summary>
        public IEnumerable<AzureOfferPlanDefinition> FixedQuantityPlans { get; set; }

        /// <summary>
        /// Flag to identify whether the meter is First Party or Third Party based on BillingEntity
        /// If the BillingEntity is SelfManaged, the Meter is First Party otherwise its a Third Party Meter
        /// </summary>
        public bool IsFirstParty
        {
            get
            {
                return (this.BillingEntity == BillingProvider.SelfManaged || 
                        this.GalleryId.ToLower().Contains("ms."));
            }
        }

        public bool Equals(OfferMeter meter)
        {
            if (meter != null)
            {
                return
                    this.AbsoluteMaximumQuantity == meter.AbsoluteMaximumQuantity &&
                    this.AssignmentModel == meter.AssignmentModel &&
                    this.BillingEntity == meter.BillingEntity &&
                    this.BillingMode == meter.BillingMode &&
                    this.BillingStartDate == meter.BillingStartDate &&
                    this.BillingState == meter.BillingState &&
                    this.Category == meter.Category &&
                    this.CommittedQuantity == meter.CommittedQuantity &&
                    this.CurrentQuantity == meter.CurrentQuantity &&
                    ((this.FixedQuantityPlans != null && meter.FixedQuantityPlans != null && Enumerable.SequenceEqual(this.FixedQuantityPlans, meter.FixedQuantityPlans)
                        || this.FixedQuantityPlans == null && meter.FixedQuantityPlans == null)) &&
                    this.GalleryId == meter.GalleryId &&
                    this.IncludedQuantity == meter.IncludedQuantity &&
                    this.MaximumQuantity == meter.MaximumQuantity &&
                    this.MeterId == meter.MeterId &&
                    this.Name == meter.Name &&
                    this.OfferScope == meter.OfferScope &&
                    this.PlatformMeterId == meter.PlatformMeterId &&
                    this.PreviewGraceDays == meter.PreviewGraceDays &&
                    this.RenewalFrequency == meter.RenewalFrequency &&
                    this.Status == meter.Status &&
                    this.TrialCycles == meter.TrialCycles &&
                    this.TrialDays == meter.TrialDays &&
                    this.Unit == meter.Unit &&
                    this.MinimumRequiredAccessLevel == meter.MinimumRequiredAccessLevel &&
                    this.IncludedInLicenseLevel == meter.IncludedInLicenseLevel &&
                    this.IsFirstParty == meter.IsFirstParty && 
                    this.AutoAssignOnAccess == meter.AutoAssignOnAccess;
            }

            return false;
        }

        public OfferMeter Clone()
        {
            OfferMeter meter = new OfferMeter()
            {
                AbsoluteMaximumQuantity = this.AbsoluteMaximumQuantity,
                AssignmentModel = this.AssignmentModel,
                BillingEntity = this.BillingEntity,
                BillingMode = this.BillingMode,
                BillingStartDate = this.BillingStartDate,
                BillingState = this.BillingState,
                Category = this.Category,
                CommittedQuantity = this.CommittedQuantity,
                CurrentQuantity = this.CurrentQuantity,
                GalleryId = this.GalleryId,
                IncludedInLicenseLevel = this.IncludedInLicenseLevel,
                IncludedQuantity = this.IncludedQuantity,
                MaximumQuantity = this.MaximumQuantity,
                MeterId = this.MeterId,
                Name = this.Name,
                OfferScope = this.OfferScope,
                PlatformMeterId = this.PlatformMeterId,
                PreviewGraceDays = this.PreviewGraceDays,
                RenewalFrequency = this.RenewalFrequency,
                Status = this.Status,
                TrialCycles = this.TrialCycles,
                TrialDays = this.TrialDays,
                Unit = this.Unit,
                MinimumRequiredAccessLevel = this.MinimumRequiredAccessLevel,
                AutoAssignOnAccess = this.AutoAssignOnAccess
            };

            if (this.FixedQuantityPlans != null)
            {
                var azureOfferPlans = new List<AzureOfferPlanDefinition>();
                meter.FixedQuantityPlans = azureOfferPlans;
                foreach (var plan in this.FixedQuantityPlans)
                {
                    azureOfferPlans.Add(plan.Clone());
                }
            }

            return meter;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as OfferMeter);
        }


        public override int GetHashCode()
        {
            return MeterId.GetHashCode();
        }

        public static bool operator == (OfferMeter left, OfferMeter right)
        {
            if ((object)left == null || (object)right == null)
                return Object.Equals(left, right);

            return left.Equals(right);
        }

        public static bool operator != (OfferMeter left, OfferMeter right)
        {
            return !(left == right);
        }
    }
}
