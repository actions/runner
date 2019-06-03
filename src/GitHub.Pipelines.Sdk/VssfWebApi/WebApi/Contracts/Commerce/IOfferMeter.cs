using System;
using System.Collections.Generic;

namespace GitHub.Services.Commerce
{
    public interface IOfferMeter
    {
        /// <summary>
        /// Gets or sets the meter identifier.
        /// </summary>
        int MeterId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the billing mode.
        /// </summary>
        ResourceBillingMode BillingMode { get; set; }

        /// <summary>
        /// Gets or sets the state of the billing.
        /// </summary>
        MeterBillingState BillingState { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        MeterCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the committed quantity.
        /// </summary>
        int CommittedQuantity { get; set; }

        /// <summary>
        /// Gets or sets the current quantity.
        /// </summary>
        int CurrentQuantity { get; set; }

        /// <summary>
        /// Gets or sets the gallery identifier.
        /// </summary>
        string GalleryId { get; set; }

        /// <summary>
        /// Gets or sets the included quantity.
        /// </summary>
        int IncludedQuantity { get; set; }

        /// <summary>
        /// Gets or sets the maximum quantity.
        /// </summary>
        int MaximumQuantity { get; set; }

        /// <summary>
        /// Gets or sets the absolute maximum quantity.
        /// </summary>
        int AbsoluteMaximumQuantity { get; set; }

        /// <summary>
        /// Gets or sets the offer scope.
        /// </summary>
        OfferScope OfferScope { get; set; }

        /// <summary>
        /// Gets or sets the platform meter identifier.
        /// </summary>
        Guid PlatformMeterId { get; set; }

        /// <summary>
        /// Gets or sets the renewal frequency.
        /// </summary>
        MeterRenewalFrequecy RenewalFrequency { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        MeterState Status { get; set; }

        /// <summary>
        /// Gets or sets the trial cycles.
        /// </summary>
        int TrialCycles { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        string Unit { get; set; }

        /// <summary>
        /// Gets or sets the user assignment model.
        /// </summary>
        OfferMeterAssignmentModel AssignmentModel { get; set; }

        /// <summary>
        /// Gets or sets the billing start date.
        /// If TrialDays + PreviewGraceDays > then, on 'BillingStartDate' it starts the preview Grace and/or trial period.
        /// </summary>
        DateTime? BillingStartDate { get; set; }

        /// <summary>
        /// Gets or sets the trial days.
        /// </summary>
        byte TrialDays { get; set; }

        /// <summary>
        /// Gets or sets the preview grace days.
        /// </summary>
        byte PreviewGraceDays { get; set; }
        
        /// <summary>
        /// Gets or sets the responsible entity/method for billing. Determines how this meter is handled in the backend.
        /// </summary>
        BillingProvider BillingEntity { get; set; }

        /// <summary>
        /// Gets or sets the minimum required access level for the meter.
        /// </summary>
        MinimumRequiredServiceLevel MinimumRequiredAccessLevel { get; set; }

        /// <summary>
        /// Gets or sets the Min license level the offer is free for.
        /// </summary>
        MinimumRequiredServiceLevel IncludedInLicenseLevel { get; set; }

        /// <summary>
        /// Gets or sets the map of named quantity varied plans, plans can be purchased that vary only in the number of users included.
        /// Null if this offer meter does not support named fixed quantity plans.
        /// </summary>
        IEnumerable<AzureOfferPlanDefinition> FixedQuantityPlans { get; set; }

        /// <summary>
        /// Flag to identify whether the meter is First Party or Third Party
        /// </summary>
        ///  <Value> 
        ///  <c>true</c> indicates its a First Party Extension
        ///  <c>false</c> indicates its a Third Party Extension
        ///  </Value> 
        bool IsFirstParty { get; }
        
        /// <summary>
        /// Indicates whether users get auto assigned this license type duing first access.
        /// </summary>
        bool AutoAssignOnAccess { get; set; }
    }
}
