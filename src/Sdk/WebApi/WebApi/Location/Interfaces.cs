using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Location;

namespace GitHub.Services.WebApi.Location
{
    /// <summary>
    /// The service responsible for providing a connection to a Team 
    /// Foundation Server as well as the locations of other services that
    /// are available on it.
    /// </summary>
    [VssClientServiceImplementation(typeof(LocationService))]
    public interface ILocationService : IVssClientService
    {
        /// <summary>
        /// Gets the provider of location data specified by the given location area guid.
        /// The provider could be local or remote depending on where the area data is hosted
        /// in the location hierarchy in relation to this service instance. Returns null if
        /// the area could not be found
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <returns></returns>
        ILocationDataProvider GetLocationData(Guid locationAreaIdentifier);

        /// <summary>
        /// Gets the URL of the location service for the given location area guid and access mapping moniker.
        /// If the area could not be found this method will return null. This is useful for getting the
        /// base URL of service hosts, or of other service instances or resource areas.
        /// 
        /// To find a specific service definition contained in the given location area and to formulate
        /// the proper URL for a specific resource in that location area, you would need to
        /// retrieve the location data for that area. This operation is simplified by calling GetLocationData
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <returns></returns>
        String GetLocationServiceUrl(Guid locationAreaIdentifier);

        /// <summary>
        /// Gets the URL of the location service for the given location area guid and access mapping moniker.
        /// If the area could not be found this method will return null. This is useful for getting the
        /// base URL of service hosts, or of other service instances or resource areas.
        /// 
        /// To find a specific service definition contained in the given location area and to formulate
        /// the proper URL for a specific resource in that location area, you would need to
        /// retrieve the location data for that area. This operation is simplified by calling GetLocationData
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <param name="accessMappingMoniker"></param>
        /// <returns></returns>
        String GetLocationServiceUrl(Guid locationAreaIdentifier, String accessMappingMoniker);

        #region Async APIs

        /// <summary>
        /// Gets the provider of location data specified by the given location area guid.
        /// The provider could be local or remote depending on where the area data is hosted
        /// in the location hierarchy in relation to this service instance. Returns null if
        /// the area could not be found
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ILocationDataProvider> GetLocationDataAsync(
            Guid locationAreaIdentifier,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the URL of the location service for the given location area guid and access mapping moniker.
        /// If the area could not be found this method will return null. This is useful for getting the
        /// base URL of service hosts, or of other service instances or resource areas.
        /// 
        /// To find a specific service definition contained in the given location area and to formulate
        /// the proper URL for a specific resource in that location area, you would need to
        /// retrieve the location data for that area. This operation is simplified by calling GetLocationData
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <param name="accessMappingMoniker"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<String> GetLocationServiceUrlAsync(
            Guid locationAreaIdentifier,
            String accessMappingMoniker = null,
            CancellationToken cancellationToken = default(CancellationToken));

        #endregion
    }

    /// <summary>
    /// The service responsible for providing a connection to a Team 
    /// Foundation Server as well as the locations of other services that
    /// are available on it.
    /// </summary>
    public interface ILocationDataProvider
    {
        /// <summary>
        /// The unique identifier for this server.
        /// </summary>
        Guid InstanceId { get; }


        /// <summary>
        /// The identifier of the type of server instance.
        /// </summary>
        Guid InstanceType { get; }

        /// <summary>
        /// The AccessMapping for the current connection to the server. Note, it is 
        /// possible that the current ClientAccessMapping is not a member of the 
        /// ConfiguredAccessMappings if the access point this client used to connect to 
        /// the server has not been configured on it. This will never be null.
        /// </summary>
        AccessMapping ClientAccessMapping { get; }

        /// <summary>
        /// The default AccessMapping for this location service. This will never be null.
        /// </summary>
        AccessMapping DefaultAccessMapping { get; }

        /// <summary>
        /// All of the AccessMappings that this location service knows about. Because a 
        /// given location service can inherit AccessMappings from its parent these 
        /// AccessMappings may exist on this location service or its parent.
        /// </summary>
        IEnumerable<AccessMapping> ConfiguredAccessMappings { get; }

        // <summary>
        // Saves the provided ServiceDefinition within the location service. This 
        // operation will assign the Identifier property on the ServiceDefinition object 
        // if one is not already assigned. Any AccessMappings referenced in the 
        // LocationMappings property must already be configured with the location 
        // service.
        // </summary>
        // <param name="serviceDefinition">
        //     The ServiceDefinition to save. This object will be updated with a new 
        //     Identifier if one is not already assigned.
        // </param>
        //void SaveServiceDefinition(
        //    ServiceDefinition serviceDefinition);

        // <summary>
        // Saves the provided ServiceDefinitions within the location service. This 
        // operation will assign the Identifier property on the ServiceDefinition 
        // objects if one is not already assigned. Any AccessMappings referenced in 
        // the LocationMappings property must already be configured with the location 
        // service.
        // </summary>
        // <param name="serviceDefinitions">
        //     The ServiceDefinitions to save. These objects will be updated with a new 
        //     Identifier if one is not already assigned.
        // </param>
        //void SaveServiceDefinitions(
        //    IEnumerable<ServiceDefinition> serviceDefinitions);

        // <summary>
        // Removes the ServiceDefinition with the specified service type and
        // service identifier from the location serivce.
        // </summary>
        // <param name="serviceType">
        //     The service type of the ServiceDefinition to remove.
        // </param>
        // <param name="serviceIdentifier">
        //     The service identifier of the ServiceDefinition to remove.
        // </param>
        //void RemoveServiceDefinition(
        //    String serviceType,
        //    Guid serviceIdentifier);

        // <summary>
        // Removes the specified ServiceDefinition from the location service.
        // </summary>
        // <param name="serviceDefinition">
        //     The ServiceDefinition to remove. This must be a ServiceDefinition that is 
        //     already registered in the location service. 
        //     Equality is decided by matching the service type and the identifier.
        // </param>
        //void RemoveServiceDefinition(
        //    ServiceDefinition serviceDefinition);

        // <summary>
        //     Removes the specified ServiceDefinitions from the location service.
        // </summary>
        // <param name="serviceDefinitions">
        //     The ServiceDefinitions to remove. These must be ServiceDefinitions that are 
        //     already registered in the location service. 
        //     Equality is decided by matching the service type and  the identifier.
        // </param>
        //void RemoveServiceDefinitions(
        //    IEnumerable<ServiceDefinition> serviceDefinitions);

        /// <summary>
        /// Finds the ServiceDefinition with the specified service type and service 
        /// identifier. If no matching ServiceDefinition exists, null is returned.
        /// </summary>
        /// <param name="serviceType">
        ///     The service type of the ServiceDefinition to find.
        /// </param>
        /// <param name="serviceIdentifier">
        ///     The service identifier of the ServiceDefinition 
        ///     to find.
        /// </param>
        /// <returns>
        ///     The ServiceDefinition with the specified service type and service identifier.
        ///     If no matching ServiceDefinition exists, null is returned.
        /// </returns>
        ServiceDefinition FindServiceDefinition(
            String serviceType,
            Guid serviceIdentifier);

        /// <summary>
        /// Finds the ServiceDefinitions for all of the services with the 
        /// specified service type. If no ServiceDefinitions of this type 
        /// exist, an empty enumeration will be returned.
        /// </summary>
        /// <param name="serviceType">
        ///     The case-insensitive string that identifies what type of service is being 
        ///     requested. If this value is null, ServiceDefinitions for all services 
        ///     registered with this location service will be returned.
        /// </param>
        /// <returns>
        ///     ServiceDefinitions for all of the services with the specified service type.
        ///     If no ServiceDefinitions of this type exist, an empty enumeration will be 
        ///     returned.
        /// </returns>
        IEnumerable<ServiceDefinition> FindServiceDefinitions(
            String serviceType);

        /// <summary>
        /// Returns the location for the ServiceDefintion associated with the ServiceType
        /// and ServiceIdentifier that should be used based on the current connection. 
        /// If a ServiceDefinition with the ServiceType and ServiceIdentifier does not
        /// exist then null will be returned. If a ServiceDefinition with the ServiceType
        /// and ServiceIdentifier is found then a location will be returned if the 
        /// ServiceDefinition is well formed (otherwise an exception will be thrown).
        /// 
        /// When determining what location to return for the ServiceDefinition and 
        /// current connection the following rules will be applied:
        /// 
        /// 1. Try to find a location for the ClientAccessMapping.
        /// 2. Try to find a location for the DefaultAccessMapping.
        /// 3. Use the first location in the LocationMappings list.
        /// </summary>
        /// <param name="serviceType">
        ///     The service type of the ServiceDefinition to find the location for.
        /// </param>
        /// <param name="serviceIdentifier">
        ///     The service identifier of the ServiceDefinition to find the location for.
        /// </param>
        /// <returns>
        ///     The location for the ServiceDefinition with the provided service type and 
        ///     identifier that should be used based on the current connection.
        /// </returns>
        String LocationForCurrentConnection(
            String serviceType,
            Guid serviceIdentifier);        

        /// <summary>
        /// Returns the location for the ServiceDefintion that should be used based on
        /// the current connection. This method will never return null or empty. If it
        /// succeeds it will return a targetable location for the provided 
        /// ServiceDefinition.
        /// 
        /// When determining what location to return for the ServiceDefinition and 
        /// current connection the following rules will be applied:
        /// 
        /// 1. Try to find a location for the ClientAccessMapping.
        /// 2. Try to find a location for the DefaultAccessMapping.
        /// 3. Use the first location in the LocationMappings list.
        /// </summary>
        /// <param name="serviceDefinition">
        ///     The ServiceDefinition to find the location for.
        /// </param>
        /// <returns>
        ///     The location for the given ServiceDefinition that should be 
        ///     used based on the current connection.
        /// </returns>
        String LocationForCurrentConnection(
            ServiceDefinition serviceDefinition);        

        /// <summary>
        /// Returns the location for the ServiceDefinition that has the specified
        /// service type and service identifier for the provided 
        /// AccessMapping. If this ServiceDefinition is FullyQualified and no 
        /// LocationMapping exists for this AccessMapping then null will be returned.
        /// </summary>
        /// <param name="serviceType">
        ///     The service type of the ServiceDefinition to find the location for.
        /// </param>
        /// <param name="serviceIdentifier">
        ///     The service identifier of the ServiceDefinition to find the location for.
        /// </param>
        /// <param name="accessMapping">The AccessMapping to find the location for.</param>
        /// <returns>
        ///     The location for the ServiceDefinition for the provided 
        ///     AccessMapping. If this ServiceDefinition is FullyQualified and no 
        ///     LocationMapping exists for this AccessMapping then null will be returned.
        /// </returns>
        String LocationForAccessMapping(
            String serviceType,
            Guid serviceIdentifier,
            AccessMapping accessMapping);        

        /// <summary>
        /// Returns the location for the ServiceDefinition for the provided 
        /// AccessMapping. If this ServiceDefinition is FullyQualified and no 
        /// LocationMapping exists for this AccessMapping then null will be returned.
        /// </summary>
        /// <param name="serviceDefinition">
        ///     The ServiceDefinition to find the location for.
        /// </param>
        /// <param name="accessMapping">The AccessMapping to find the location for.</param>
        /// <returns>
        ///     The location for the ServiceDefinition for the provided 
        ///     AccessMapping. If this ServiceDefinition is FullyQualified and no 
        ///     LocationMapping exists for this AccessMapping then null will be returned.
        /// </returns>
        String LocationForAccessMapping(
            ServiceDefinition serviceDefinition,
            AccessMapping accessMapping);

        // <summary>
        // Configures the AccessMapping with the provided moniker to have the provided 
        // display name and access point. This function also allows for this 
        // AccessMapping to be made the default AccessMapping.
        // </summary>
        // <param name="moniker">
        //     A string that uniquely identifies this AccessMapping. This value cannot be 
        //     null or empty.
        // </param>
        // <param name="displayName">
        //     Display name for this AccessMapping. This value cannot be null or empty. 
        // </param>
        // <param name="accessPoint">
        //     This is the base url for the server that will map to  this AccessMapping. 
        //     This value cannot be null or empty. 
        //     
        //     The access point should consist of the scheme, authority, port and web 
        //     application virtual path of the targetable server address. For example, an 
        //     access point will most commonly look like this:
        //     
        //     http://server:8080/tfs/
        // </param>
        // <param name="makeDefault">
        //     If true, this AccessMapping will be made the default AccessMapping. If false,
        //     the default AccessMapping will not change.
        // </param>
        // <returns>The AccessMapping object that was just configured.</returns>
        //AccessMapping ConfigureAccessMapping(
        //    String moniker,
        //    String displayName,
        //    String accessPoint,
        //    Boolean makeDefault);

        // <summary>
        // Sets the default AccessMapping to the AccessMapping passed in.
        // </summary>
        // <param name="accessMapping">
        //     The AccessMapping that should become the default AccessMapping. This 
        //     AccessMapping must already be configured with this location service.
        // </param>
        //void SetDefaultAccessMapping(
        //    AccessMapping accessMapping);

        /// <summary>
        /// Gets the AccessMapping with the specified moniker. Returns null
        /// if an AccessMapping with the supplied moniker does not exist.
        /// </summary>
        /// <param name="moniker">
        ///     The moniker for the desired AccessMapping. This value cannot be null or 
        ///     empty.
        /// </param>
        /// <returns>
        ///     The AccessMapping with the supplied moniker or null if one does not exist.
        /// </returns>
        AccessMapping GetAccessMapping(
            String moniker);        

        // <summary>
        // Removes an AccessMapping and all of the locations that are mapped
        // to it within ServiceDefinitions.
        // </summary>
        // <param name="moniker">The moniker for the AccessMapping to remove.</param>
        //void RemoveAccessMapping(
        //    String moniker);

        /// <summary>
        /// Get the API resource locations -- a collection of versioned URL paths that
        /// are keyed by a location identitifer
        /// </summary>
        /// <returns></returns>
        ApiResourceLocationCollection GetResourceLocations();

        #region Async APIs

        /// <summary>
        /// The unique identifier for this server.
        /// </summary>
        Task<Guid> GetInstanceIdAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// The identifier of the type of server instance.
        /// </summary>
        Task<Guid> GetInstanceTypeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// The AccessMapping for the current connection to the server. Note, it is 
        /// possible that the current ClientAccessMapping is not a member of the 
        /// ConfiguredAccessMappings if the access point this client used to connect to 
        /// the server has not been configured on it. This will never be null.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AccessMapping> GetClientAccessMappingAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// The default AccessMapping for this location service. This will never be null.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AccessMapping> GetDefaultAccessMappingAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// All of the AccessMappings that this location service knows about. Because a 
        /// given location service can inherit AccessMappings from its parent these 
        /// AccessMappings may exist on this location service or its parent.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<AccessMapping>> GetConfiguredAccessMappingsAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds the ServiceDefinition with the specified service type and service 
        /// identifier. If no matching ServiceDefinition exists, null is returned.
        /// </summary>
        /// <param name="serviceType">
        ///     The service type of the ServiceDefinition to find.
        /// </param>
        /// <param name="serviceIdentifier">
        ///     The service identifier of the ServiceDefinition 
        ///     to find.
        /// </param>
        /// <returns>
        ///     The ServiceDefinition with the specified service type and service identifier.
        ///     If no matching ServiceDefinition exists, null is returned.
        /// </returns>
        Task<ServiceDefinition> FindServiceDefinitionAsync(
            String serviceType,
            Guid serviceIdentifier,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Finds the ServiceDefinitions for all of the services with the 
        /// specified service type. If no ServiceDefinitions of this type 
        /// exist, an empty enumeration will be returned.
        /// </summary>
        /// <param name="serviceType">
        ///     The case-insensitive string that identifies what type of service is being 
        ///     requested. If this value is null, ServiceDefinitions for all services 
        ///     registered with this location service will be returned.
        /// </param>
        /// <returns>
        ///     ServiceDefinitions for all of the services with the specified service type.
        ///     If no ServiceDefinitions of this type exist, an empty enumeration will be 
        ///     returned.
        /// </returns>
        Task<IEnumerable<ServiceDefinition>> FindServiceDefinitionsAsync(
            String serviceType,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the location for the ServiceDefintion associated with the ServiceType
        /// and ServiceIdentifier that should be used based on the current connection. 
        /// If a ServiceDefinition with the ServiceType and ServiceIdentifier does not
        /// exist then null will be returned. If a ServiceDefinition with the ServiceType
        /// and ServiceIdentifier is found then a location will be returned if the 
        /// ServiceDefinition is well formed (otherwise an exception will be thrown).
        /// 
        /// When determining what location to return for the ServiceDefinition and 
        /// current connection the following rules will be applied:
        /// 
        /// 1. Try to find a location for the ClientAccessMapping.
        /// 2. Try to find a location for the DefaultAccessMapping.
        /// 3. Use the first location in the LocationMappings list.
        /// </summary>
        /// <param name="serviceType">
        ///     The service type of the ServiceDefinition to find the location for.
        /// </param>
        /// <param name="serviceIdentifier">
        ///     The service identifier of the ServiceDefinition to find the location for.
        /// </param>
        /// <returns>
        ///     The location for the ServiceDefinition with the provided service type and 
        ///     identifier that should be used based on the current connection.
        /// </returns>
        Task<String> LocationForCurrentConnectionAsync(
            String serviceType,
            Guid serviceIdentifier,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the location for the ServiceDefintion that should be used based on
        /// the current connection. This method will never return null or empty. If it
        /// succeeds it will return a targetable location for the provided 
        /// ServiceDefinition.
        /// 
        /// When determining what location to return for the ServiceDefinition and 
        /// current connection the following rules will be applied:
        /// 
        /// 1. Try to find a location for the ClientAccessMapping.
        /// 2. Try to find a location for the DefaultAccessMapping.
        /// 3. Use the first location in the LocationMappings list.
        /// </summary>
        /// <param name="serviceDefinition">
        ///     The ServiceDefinition to find the location for.
        /// </param>
        /// <returns>
        ///     The location for the given ServiceDefinition that should be 
        ///     used based on the current connection.
        /// </returns>
        Task<String> LocationForCurrentConnectionAsync(
            ServiceDefinition serviceDefinition,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the location for the ServiceDefinition that has the specified
        /// service type and service identifier for the provided 
        /// AccessMapping. If this ServiceDefinition is FullyQualified and no 
        /// LocationMapping exists for this AccessMapping then null will be returned.
        /// </summary>
        /// <param name="serviceType">
        ///     The service type of the ServiceDefinition to find the location for.
        /// </param>
        /// <param name="serviceIdentifier">
        ///     The service identifier of the ServiceDefinition to find the location for.
        /// </param>
        /// <param name="accessMapping">The AccessMapping to find the location for.</param>
        /// <returns>
        ///     The location for the ServiceDefinition for the provided 
        ///     AccessMapping. If this ServiceDefinition is FullyQualified and no 
        ///     LocationMapping exists for this AccessMapping then null will be returned.
        /// </returns>
        Task<String> LocationForAccessMappingAsync(
            String serviceType,
            Guid serviceIdentifier,
            AccessMapping accessMapping,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the location for the ServiceDefinition for the provided 
        /// AccessMapping. If this ServiceDefinition is FullyQualified and no 
        /// LocationMapping exists for this AccessMapping then null will be returned.
        /// </summary>
        /// <param name="serviceDefinition">
        ///     The ServiceDefinition to find the location for.
        /// </param>
        /// <param name="accessMapping">The AccessMapping to find the location for.</param>
        /// <returns>
        ///     The location for the ServiceDefinition for the provided 
        ///     AccessMapping. If this ServiceDefinition is FullyQualified and no 
        ///     LocationMapping exists for this AccessMapping then null will be returned.
        /// </returns>
        Task<String> LocationForAccessMappingAsync(
            ServiceDefinition serviceDefinition,
            AccessMapping accessMapping,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the AccessMapping with the specified moniker. Returns null
        /// if an AccessMapping with the supplied moniker does not exist.
        /// </summary>
        /// <param name="moniker">
        ///     The moniker for the desired AccessMapping. This value cannot be null or 
        ///     empty.
        /// </param>
        /// <returns>
        ///     The AccessMapping with the supplied moniker or null if one does not exist.
        /// </returns>
        Task<AccessMapping> GetAccessMappingAsync(
            String moniker,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the API resource locations -- a collection of versioned URL paths that
        /// are keyed by a location identitifer
        /// </summary>
        /// <returns></returns>
        Task<ApiResourceLocationCollection> GetResourceLocationsAsync(CancellationToken cancellationToken = default(CancellationToken));

        #endregion
    }
}
