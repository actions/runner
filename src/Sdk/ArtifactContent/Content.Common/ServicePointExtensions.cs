using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

public static class ServicePointExtensions
{
    private static ConcurrentDictionary<string, ServicePointConfig> cache = new ConcurrentDictionary<string, ServicePointConfig>();
    private static Object Lock = new Object();

    public static IEnumerable<string> GetConfigCacheKeys()
    {
        lock (Lock)
        {
            return cache.Keys.ToArray();
        }
    }

    public struct ServicePointConfigKeepAlive
    {
        public TimeSpan KeepAliveTime;
        public TimeSpan KeepAliveInterval;
    }

    public struct ServicePointConfig
    {
        public int? MaxConnectionsPerProcessor;
        public TimeSpan? ConnectionLeaseTimeout;
        public bool? UseNagleAlgorithm;
        public bool? Expect100Continue;
        public ServicePointConfigKeepAlive? TcpKeepAlive;
        public TimeSpan? MaxIdleTime;

        public static ServicePointConfig Update(ServicePointConfig a, ServicePointConfig b)
        {
            return new ServicePointConfig
            {
                MaxConnectionsPerProcessor = b.MaxConnectionsPerProcessor.HasValue ? b.MaxConnectionsPerProcessor : a.MaxConnectionsPerProcessor,
                ConnectionLeaseTimeout = b.ConnectionLeaseTimeout.HasValue ? b.ConnectionLeaseTimeout : a.ConnectionLeaseTimeout,
                UseNagleAlgorithm = b.UseNagleAlgorithm.HasValue ? b.UseNagleAlgorithm : a.UseNagleAlgorithm,
                Expect100Continue = b.Expect100Continue.HasValue ? b.Expect100Continue : a.Expect100Continue,
                TcpKeepAlive = b.TcpKeepAlive.HasValue ? b.TcpKeepAlive : a.TcpKeepAlive,
                MaxIdleTime = b.MaxIdleTime.HasValue ? b.MaxIdleTime : a.MaxIdleTime
            };
        }
    }

    // The following can cause races. Do we care? If so, we have to enclose this in a lock, which would probably be fine.
    //
    public static void UpdateServicePointSettings(this ServicePoint servicePointToModify, ServicePointConfig config)
    {
        string key = MakeQueryString(servicePointToModify.Address);
        var cachedConfig = new ServicePointConfig();

        cache.TryGetValue(key, out cachedConfig);
        var updatedConfig = ServicePointConfig.Update(cachedConfig, config);
        if (!cachedConfig.Equals(updatedConfig))
        {
            lock (Lock)
            {
                cache.TryGetValue(key, out cachedConfig);
                updatedConfig = ServicePointConfig.Update(cachedConfig, config);
                if (!cachedConfig.Equals(updatedConfig))
                {
                    if (cachedConfig.MaxConnectionsPerProcessor != updatedConfig.MaxConnectionsPerProcessor)
                    {
                        cache.AddOrUpdate(key, updatedConfig, (k, v) => { v.MaxConnectionsPerProcessor = updatedConfig.MaxConnectionsPerProcessor; return v; });
                        servicePointToModify.ConnectionLimit = updatedConfig.MaxConnectionsPerProcessor.Value * Environment.ProcessorCount;
                    }
                    if (cachedConfig.ConnectionLeaseTimeout != updatedConfig.ConnectionLeaseTimeout)
                    {
                        cache.AddOrUpdate(key, updatedConfig, (k, v) => { v.ConnectionLeaseTimeout = updatedConfig.ConnectionLeaseTimeout; return v; });
                        servicePointToModify.ConnectionLeaseTimeout = (int)updatedConfig.ConnectionLeaseTimeout.Value.TotalMilliseconds;
                    }
                    if (cachedConfig.UseNagleAlgorithm != updatedConfig.UseNagleAlgorithm)
                    {
                        cache.AddOrUpdate(key, updatedConfig, (k, v) => { v.UseNagleAlgorithm = updatedConfig.UseNagleAlgorithm; return v; });
                        servicePointToModify.UseNagleAlgorithm = updatedConfig.UseNagleAlgorithm.Value;
                    }
                    if (cachedConfig.Expect100Continue != updatedConfig.Expect100Continue)
                    {
                        cache.AddOrUpdate(key, updatedConfig, (k, v) => { v.Expect100Continue = updatedConfig.Expect100Continue; return v; });
                        servicePointToModify.Expect100Continue = updatedConfig.Expect100Continue.Value;
                    }
                    if (!cachedConfig.TcpKeepAlive.Equals(updatedConfig.TcpKeepAlive))
                    {
                        cache.AddOrUpdate(key, updatedConfig, (k, v) => { v.TcpKeepAlive = updatedConfig.TcpKeepAlive; return v; });
                        servicePointToModify.SetTcpKeepAlive(
                            enabled: true,
                            keepAliveTime: (int)updatedConfig.TcpKeepAlive.Value.KeepAliveTime.TotalMilliseconds,
                            keepAliveInterval: (int)updatedConfig.TcpKeepAlive.Value.KeepAliveInterval.TotalMilliseconds);
                    }
                    if (cachedConfig.MaxIdleTime != updatedConfig.MaxIdleTime)
                    {
                        cache.AddOrUpdate(key, updatedConfig, (k, v) => { v.MaxIdleTime = updatedConfig.MaxIdleTime; return v; });
                        servicePointToModify.MaxIdleTime = (int)updatedConfig.MaxIdleTime.Value.TotalMilliseconds;
                    }
                }
            }
        }
    }

    public static void UpdateServicePointSettings(this ServicePoint servicePointToModify, int maxConnectionsPerProcessor, TimeSpan? connectionLeaseTimeout = null)
    {
        var newConfig = new ServicePointConfig
        {
            MaxConnectionsPerProcessor = maxConnectionsPerProcessor,
            ConnectionLeaseTimeout = connectionLeaseTimeout,
            Expect100Continue = false,
            UseNagleAlgorithm = false,
            TcpKeepAlive = new ServicePointConfigKeepAlive
            {
                KeepAliveTime = TimeSpan.FromSeconds(30),
                KeepAliveInterval = TimeSpan.FromSeconds(5)
            }
        };

        UpdateServicePointSettings(servicePointToModify, newConfig);
    }

    // CODESYNC ServicePointManager https://referencesource.microsoft.com/#System/net/System/Net/ServicePointManager.cs,86ab68f9e9462330
    private static string MakeQueryString(Uri address)
    {
        if (address.IsDefaultPort)
            return address.Scheme + "://" + address.DnsSafeHost;
        else
            return address.Scheme + "://" + address.DnsSafeHost + ":" + address.Port.ToString();
    }
}
