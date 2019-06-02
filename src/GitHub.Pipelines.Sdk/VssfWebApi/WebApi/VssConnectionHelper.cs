using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Location;
using Microsoft.VisualStudio.Services.Organization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Helper methods for connecting to VSTS resources.
    /// </summary>
    public static class VssConnectionHelper
    {
        /// <summary>
        /// Gets the connection URL for the specified VSTS organization name.
        /// </summary>
        public static async Task<Uri> GetOrganizationUrlAsync(string organizationName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await s_organizationHelper.GetUrlAsync(organizationName, cancellationToken);
        }

        /// <summary>
        /// Gets the connection URL for the specified VSTS organization ID.
        /// </summary>
        public static async Task<Uri> GetOrganizationUrlAsync(Guid organizationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await s_organizationHelper.GetUrlAsync(organizationId, cancellationToken);
        }

        private static readonly OrganizationHelper s_organizationHelper = new OrganizationHelper("https://app.vssps.visualstudio.com", Guid.Parse("79134C72-4A58-4B42-976C-04E7115F32BF"));

        internal class OrganizationHelper
        {
            public OrganizationHelper(string locationServiceUrl, Guid resourceAreaId)
            {
                m_locationServiceUrl = locationServiceUrl;
                m_resourceAreaId = resourceAreaId;
                m_client = new HttpClient();
            }

            public async Task<Uri> GetUrlAsync(string organizationName, CancellationToken cancellationToken = default(CancellationToken))
            {
                ArgumentUtility.CheckStringForNullOrEmpty(organizationName, nameof(organizationName));

                string requestUrl = $"{m_locationServiceUrl}/_apis/resourceAreas/{m_resourceAreaId}/?accountName={organizationName}&api-version=5.0-preview.1";

                HttpResponseMessage response = await m_client.GetAsync(requestUrl, cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    ResourceAreaInfo value = await response.Content.ReadAsAsync<ResourceAreaInfo>(cancellationToken);

                    if (value != null)
                    {
                        return new Uri(value.LocationUrl);
                    }
                }

                throw new OrganizationNotFoundException(organizationName);
            }

            public async Task<Uri> GetUrlAsync(Guid organizationId, CancellationToken cancellationToken = default(CancellationToken))
            {
                ArgumentUtility.CheckForEmptyGuid(organizationId, nameof(organizationId));

                string requestUrl = $"{m_locationServiceUrl}/_apis/resourceAreas/{m_resourceAreaId}/?hostId={organizationId}&api-version=5.0-preview.1";

                HttpResponseMessage response = await m_client.GetAsync(requestUrl, cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    ResourceAreaInfo value = await response.Content.ReadAsAsync<ResourceAreaInfo>(cancellationToken);

                    if (value != null)
                    {
                        return new Uri(value.LocationUrl);
                    }
                }

                throw new OrganizationNotFoundException(organizationId.ToString());
            }

            private readonly HttpClient m_client;
            private readonly string m_locationServiceUrl;
            private readonly Guid m_resourceAreaId;
        }
    }
}
