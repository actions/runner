using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Provided by the Azure marketplace. Do not change this.
    /// </summary>
    public class MarketplaceOfferContract2
    {
        [JsonProperty("assetDetails")]
        public AssetDetails AssetDetails { get; set; }

        [JsonProperty("assetId")]
        public string AssetId { get; set; }

        [JsonProperty("assetVersion")]
        public long AssetVersion { get; set; }

        [JsonProperty("customerSupportEmail")]
        public string CustomerSupportEmail { get; set; }

        [JsonProperty("customerSupportPhoneNumber")]
        public string CustomerSupportPhoneNumber { get; set; }

        [JsonProperty("integrationContactEmail")]
        public string IntegrationContactEmail { get; set; }

        [JsonProperty("integrationContactPhoneNumber")]
        public string IntegrationContactPhoneNumber { get; set; }

        [JsonProperty("operation")]
        public RESTApiRequestOperationType2 Operation { get; set; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("publishVersion")]
        public long PublishVersion { get; set; }

        [JsonProperty("publisherDisplayName")]
        public string PublisherDisplayName { get; set; }

        [JsonProperty("publisherId")]
        public string PublisherId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("operationStatus")]
        public RestApiResponseStatusModel OperationStatus { get; set; }
    }

    public class AssetDetails
    {
        [JsonProperty("ChangedTime")]
        public DateTime ChangedTime { get; set; }

        [JsonProperty("Definition")]
        public Definition Definition { get; set; }

        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("OfferTypeChange")]
        public long OfferTypeChange { get; set; }

        [JsonProperty("OfferTypeId")]
        public string OfferTypeId { get; set; }

        [JsonProperty("OfferTypeVersions")]
        public OfferTypeVersions OfferTypeVersions { get; set; }

        [JsonProperty("PricingNotRecalculatedRegions")]
        public List<object> PricingNotRecalculatedRegions { get; set; }

        [JsonProperty("PublisherId")]
        public string PublisherId { get; set; }

        [JsonProperty("Status")]
        public long Status { get; set; }

        [JsonProperty("Version")]
        public long Version { get; set; }
    }

    public class OfferTypeVersions
    {
        [JsonProperty("vs-marketplace-extensions")]
        public long VsMarketplaceExtensions { get; set; }
    }

    public class Definition
    {
        [JsonProperty("DisplayText")]
        public string DisplayText { get; set; }

        [JsonProperty("LocalizedValues")]
        public object LocalizedValues { get; set; }

        [JsonProperty("plans")]
        public List<OfferPlan> Plans { get; set; }
    }

    public class OfferPlan
    {
        [JsonProperty("monthlyPricing")]
        public MonthlyPricing MonthlyPricing { get; set; }

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("vs-marketplace-extensions.skuDescription")]
        public string VsMarketplaceExtensionsSkuDescription { get; set; }

        [JsonProperty("vs-marketplace-extensions.skuSummary")]
        public string VsMarketplaceExtensionsSkuSummary { get; set; }

        [JsonProperty("vs-marketplace-extensions.skuTitle")]
        public string VsMarketplaceExtensionsSkuTitle { get; set; }

        [JsonProperty("vs-marketplace-extensions.skuUsers")]
        public int VsMarketplaceExtensionsSkuUsers { get; set; }
    }

    public class MonthlyPricing
    {
        [JsonProperty("multiplier")]
        public Multiplier Multiplier { get; set; }

        [JsonProperty("regionPrices")]
        public Dictionary<string, RegionPrice> RegionPrices { get; set; }

        [JsonProperty("regions")]
        public List<string> Regions { get; set; }
    }

    public class RegionPrice
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }
    }

    public class Multiplier
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("single")]
        public long Single { get; set; }
    }

    public enum RESTApiRequestOperationType2
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

    public class RestApiResponseStatusModel2
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

    public class MicrosoftAzureMarketplaceLeadConfiguration
    {
    }
}
