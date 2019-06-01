using System;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class AuthorizationScopeDescription
    {
        public static readonly string FallbackMarket = string.Empty;

        public AuthorizationScopeDescription(string market, string title, string description)
        {
            if (market == null)
            {
                throw new ArgumentNullException(nameof(market));
            }

            if (market != FallbackMarket && string.IsNullOrWhiteSpace(market))
            {
                throw new ArgumentException(string.Concat("Market is required: '", market, "'"));
            }

            if (market != FallbackMarket)
            {
                if (string.IsNullOrWhiteSpace(market))
                {
                    throw new ArgumentException(string.Concat("Market is required: '", market, "'"));
                }
            }

            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(string.Concat("Title is required: '", title, "'"));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException(string.Concat("Description is required: '", description, "'"));
            }

            this.Market = market;
            this.Title = title;
            this.Description = description;
        }

        public string Market { get; protected set; }

        public string Title { get; protected set; }

        public string Description { get; protected set; }
    }
}
