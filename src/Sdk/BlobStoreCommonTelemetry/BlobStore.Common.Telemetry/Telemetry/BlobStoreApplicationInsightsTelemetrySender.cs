using System;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.Services.Content.Common.Telemetry;
using GitHub.Services.Content.Common.Tracing;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.BlobStore.Common.Telemetry
{
    /// <summary>
    /// Creates a new ApplicationInsightsTelemetrySender with the InstrumentationKey read from the BlobStore API.
    /// </summary>
    public class BlobStoreApplicationInsightsTelemetrySender : ApplicationInsightsTelemetrySender
    {
        internal readonly Guid InstrumentationKey = Guid.Empty;
        private const string InstrumentationKeyEndPoint = "_apis/pipelineartifactstelemetry/aiinstrumentationkey?api-version=5.2-preview";
        private const string TokenKey = "instrumentationkey";
        private static readonly HttpClient basicHttpClient = new HttpClient();
        private Uri baseAddress;

        public BlobStoreApplicationInsightsTelemetrySender(IAppTraceSource tracer, Uri baseAddress)
            : base(Guid.Empty.ToString(), tracer)
        {
            this.baseAddress = baseAddress;
            string instrumentationKey = GetInstrumentationKeyAsync().Result;
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                Guid.TryParse(instrumentationKey, out InstrumentationKey);
            }
            // Sets base.instrumentationKey and base.client.instrumentationKey.
            // Historically these were passed in directly, however since we need to
            // retrieve the instrumentationKey at runtime, we'll need to update throughout.
            UpdateInstrumentationKey(InstrumentationKey.ToString());
        }

        /// <summary>
        /// Hits the <see cref="InstrumentationKeyEndPoint"/>
        /// </summary>
        /// <returns>The <see cref="InstrumentationKey"/> if it exists</returns>
        private async Task<string> GetInstrumentationKeyAsync()
        {
            string key = string.Empty;
            if (baseAddress == null)
            {
                return key;
            }

            // e.g. "https://vsblob1.vsblob.vsts.me/Ab577a6e7-60c9-4178-846d-0b698bd87f25/_apis/pipelineartifactstelemetry/aiinstrumentationkey"
            Uri instrumentationKeyUri = new Uri(baseAddress, InstrumentationKeyEndPoint);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, instrumentationKeyUri);
            try
            {
                HttpResponseMessage response = await basicHttpClient.SendAsync(request).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JToken token = JToken.Parse(content);
                    if (token[TokenKey] != null)
                    {
                        key = token[TokenKey].ToString();
                    }
                }
            }
            catch (HttpRequestException e)
            {
                // Don't interrupt if the request fails
                tracer.Verbose($"HttpRequestException: {e.Message}. Telemetry will not be sent to Application Insights. Suppressing.");
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                // Don't interrupt if there's a parsing error
                tracer.Verbose($"JsonReaderException: {e.Message}. Telemetry will not be sent to Application Insights. Suppressing.");
            }
            return key;
        }
    }
}
