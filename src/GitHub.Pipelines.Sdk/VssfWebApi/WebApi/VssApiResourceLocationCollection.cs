using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Container for API resource locations
    /// </summary>
    public class ApiResourceLocationCollection
    {
        private Dictionary<Guid, ApiResourceLocation> m_locationsById = new Dictionary<Guid, ApiResourceLocation>();
        private Dictionary<String, List<ApiResourceLocation>> m_locationsByKey = new Dictionary<String, List<ApiResourceLocation>>();

        /// <summary>
        /// Add a new API resource location
        /// </summary>
        /// <param name="location">API resource location to add</param>
        public void AddResourceLocation(ApiResourceLocation location)
        {
            ApiResourceLocation existingLocation;
            if (m_locationsById.TryGetValue(location.Id, out existingLocation))
            {
                if (!location.Equals(existingLocation))  // unit tests will register same resources multiple times, so only throw if the ApiResourceLocation doesn't match what is already cached.
                {
                    throw new VssApiResourceDuplicateIdException(location.Id);
                }
            }

            m_locationsById[location.Id] = location;

            List<ApiResourceLocation> locationsByKey;
            String locationCacheKey = GetLocationCacheKey(location.Area, location.ResourceName);
            if (!m_locationsByKey.TryGetValue(locationCacheKey, out locationsByKey))
            {
                locationsByKey = new List<ApiResourceLocation>();
                m_locationsByKey.Add(locationCacheKey, locationsByKey);
            }

            if (!locationsByKey.Any(x => x.Id.Equals(location.Id)))
            {
                locationsByKey.Add(location);
            }
        }

        /// <summary>
        /// Add new API resource locations
        /// </summary>
        /// <param name="locations">API resource locations to add</param>
        public void AddResourceLocations(IEnumerable<ApiResourceLocation> locations)
        {
            if (locations != null)
            {
                foreach (ApiResourceLocation location in locations)
                {
                    AddResourceLocation(location);
                }
            }
        }

        private String GetLocationCacheKey(String area, String resourceName)
        {
            if (area == null)
            {
                area = String.Empty;
            }
            if (resourceName == null)
            {
                resourceName = String.Empty;
            }

            return String.Format("{0}:{1}", area.ToLower(), resourceName.ToLower());
        }

        /// <summary>
        /// Get an API resource location by location id. Returns null if not found.
        /// </summary>
        /// <param name="locationId">Id of the registered resource location</param>
        /// <returns>ApiResourceLocation or null if not found</returns>
        public ApiResourceLocation TryGetLocationById(Guid locationId)
        {
            ApiResourceLocation location;
            m_locationsById.TryGetValue(locationId, out location);
            return location;
        }

        /// <summary>
        /// Get an API resource location by location id. Throws if not found.
        /// </summary>
        /// <param name="locationId">Id of the registered resource location</param>
        /// <returns>ApiResourceLocation or null if not found</returns>
        public ApiResourceLocation GetLocationById(Guid locationId)
        {
            ApiResourceLocation location = TryGetLocationById(locationId);
            if (location == null)
            {
                throw new VssResourceNotFoundException(locationId);
            }
            return location;
        }

        /// <summary>
        /// Get all API resource locations
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ApiResourceLocation> GetAllLocations()
        {
            return m_locationsById.Values;
        }

        /// <summary>
        /// Get all API resource locations under a given area
        /// </summary>
        /// <param name="area">Resource area name</param>
        /// <returns></returns>
        public IEnumerable<ApiResourceLocation> GetAreaLocations(String area)
        {
            return m_locationsById.Values.Where(l => String.Equals(area, l.Area, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all API resource locations for a given resource.
        /// </summary>
        /// <remarks>Note: There are multiple locations for a given resource when multiple routes are registered for that resource</remarks>
        /// <param name="area">Resource area name</param>
        /// <param name="resourceName">Resource name</param>
        /// <returns></returns>
        public IEnumerable<ApiResourceLocation> GetResourceLocations(String area, String resourceName)
        {
            List<ApiResourceLocation> locationsByKey;
            String locationCacheKey = GetLocationCacheKey(area, resourceName);

            if (m_locationsByKey.TryGetValue(locationCacheKey, out locationsByKey))
            {
                return locationsByKey;
            }
            else
            {
                return Enumerable.Empty<ApiResourceLocation>();
            }
        }
    }
}
