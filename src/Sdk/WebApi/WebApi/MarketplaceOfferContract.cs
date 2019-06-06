using System.Collections.Generic;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Provided by the Azure marketplace. Do not change this.
    /// </summary>
    public class MarketplaceOfferContract
    {
        /// <summary>
        /// Gets or sets the resource type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the asset id
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Gets or sets the asset version
        /// </summary>
        public long AssetVersion { get; set; }

        /// <summary>
        /// Gets or sets the plan identifier if any.
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the asset version
        /// </summary>
        public RESTApiRequestOperationType Operation { get; set; }

        /// <summary>
        /// Gets or sets the customer support email
        /// </summary>
        public string CustomerSupportEmail { get; set; }

        /// <summary>
        /// Gets or sets the customer support phone number
        /// </summary>
        public string CustomerSupportPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the integration contact email
        /// </summary>
        public string IntegrationContactEmail { get; set; }

        /// <summary>
        /// Gets or sets the integration contact phone number
        /// </summary>
        public string IntegrationContactPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the asset details
        /// </summary>
        public AssetDetailObject AssetDetails { get; set; }

        public RestApiResponseStatusModel operationStatus { get; set; }
    }

    /// <summary>
    /// Structure of the asset detail object.
    /// </summary>
    public class AssetDetailObject
    {
        /// <summary>
        /// Gets or sets the offer identifier
        /// </summary>
        public string OfferMarketingUrlIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the publisher identifier
        /// </summary>
        public string PublisherNaturalIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the service natural identifier which was assumed offer name ?
        /// </summary>
        public string ServiceNaturalIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the product natural identifier which is assumed offer name
        /// </summary>
        public string ProductTypeNaturalIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the publisher id
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// Gets or sets the publisher name
        /// </summary>
        public string PublisherName { get; set; }

        /// <summary>
        /// Gets or sets the resource id
        /// </summary>
        public string OfferId { get; set; }

        /// <summary>
        /// Gets or sets the Plan details
        /// </summary>
        public Dictionary<string, PlanDetails> AnswersPerPlan { get; set; }

        /// <summary>
        /// Gets or sets the price details
        /// </summary>
        public Dictionary<string, List<object>> ServicePlansByMarket { get; set; }

        /// <summary>
        /// Gets or sets the Language fields
        /// </summary>
        public Dictionary<string, LangDetails> Languages { get; set; }

        /// <summary>
        /// Gets or sets the data inside Answers
        /// </summary>
        public AnswersDetails Answers { get; set; }
    }

    /// <summary>
    /// The contents of each plan provided by marketplace.
    /// </summary>
    public class PlanDetails
    {
        public int PlanUsers { get; set; }
    }

    /// <summary>
    /// The contents of each Language elements in the asset details.
    /// </summary>
    public class LangDetails
    {
        public string Title { get; set; }
        public int Summary { get; set; }
    }

    /// <summary>
    /// The contents of Answers inside the assetDetails
    /// </summary>
    public class AnswersDetails
    {
        /// Gets or sets the publisher name
        public string VSMarketplacePublisherName { get; set; }

        /// Gets or sets the extensionname
        public string VSMarketplaceExtensionName { get; set; }
    }

    /// <summary>
    /// The status of a REST Api request.
    /// </summary>
    public enum RESTApiRequestOperationType
    {
        /// <summary>
        /// The operation is for preview (or stage for testing).
        /// </summary>
        Preview,

        /// <summary>
        /// The operation is for production.
        /// </summary>
        Production,

        /// <summary>
        /// The operation is for hide.
        /// </summary>
        Hide,

        /// <summary>
        /// The operation is for unhide.
        /// </summary>
        Show,

        /// <summary>
        /// The operation is for delete previewed or staged assets.
        /// </summary>
        DeletePreview,

        /// <summary>
        /// The operation is for delete listed or live assets.
        /// </summary>
        DeleteProduction
    }

    public class RestApiResponseStatusModel
    {
        /// <summary>
        /// Gets or sets the operation id
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets the status
        /// </summary>
        public RestApiResponseStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the status message
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the completed status percentage
        /// </summary>
        public int PercentageCompleted { get; set; }
    }

    /// <summary>
    /// The status of a REST Api response status.
    /// </summary>
    public enum RestApiResponseStatus
    {
        /// <summary>
        /// The operation is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The operation is failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The operation is in progress.
        /// </summary>
        Inprogress,

        /// <summary>
        /// The operation is in skipped.
        /// </summary>
        Skipped
    }
}
