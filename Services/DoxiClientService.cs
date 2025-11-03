using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Consist.ProjectName.Services
{
    /// <summary>
    /// Service for managing DoxiClient instances with 10-minute caching
    /// Cache key format: {tenant}_{username}
    /// </summary>
    public class DoxiClientService : IDoxiClientService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<DoxiClientService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
        private readonly object _lockObject = new object();

        public DoxiClientService(
            IMemoryCache cache,
            ILogger<DoxiClientService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<object> GetOrCreateClientAsync(string tenant, string username, string password, string apiBaseUrl)
        {
            // Create cache key: tenant_username
            var cacheKey = $"{tenant}_{username}";

            _logger.LogDebug("Getting DoxiClient for cache key: {CacheKey}", cacheKey);

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out object? cachedClient) && cachedClient != null)
            {
                _logger.LogDebug("DoxiClient found in cache for key: {CacheKey}", cacheKey);
                return cachedClient;
            }

            _logger.LogInformation("Creating new DoxiClient instance for tenant: {Tenant}, user: {Username}", tenant, username);

            // Create new instance
            var doxiClient = await CreateDoxiClientAsync(tenant, username, password, apiBaseUrl);

            // Store in cache with 10-minute expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
                SlidingExpiration = null // Absolute expiration only
            };

            lock (_lockObject)
            {
                _cache.Set(cacheKey, doxiClient, cacheOptions);
                _logger.LogDebug("DoxiClient cached for key: {CacheKey} with {Minutes} minute expiration", cacheKey, _cacheExpiration.TotalMinutes);
            }

            return doxiClient;
        }

        private async Task<object> CreateDoxiClientAsync(string tenant, string username, string password, string apiBaseUrl)
        {
            try
            {
                // Find DoxiClient type
                var clientType = Type.GetType("Doxi.APIClient.DoxiClient, Doxi.APIClient")
                    ?? AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.Name == "DoxiClient" && t.Namespace?.Contains("Doxi") == true);

                if (clientType == null)
                {
                    _logger.LogWarning("DoxiClient type not found, returning placeholder object");
                    return new object(); // Placeholder - HTTP calls will be used instead
                }

                // Get all public constructors
                var constructors = clientType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

                if (constructors.Length == 0)
                {
                    _logger.LogWarning("No public constructors found on DoxiClient, returning placeholder object");
                    return new object();
                }

                _logger.LogDebug("Found {Count} constructors on DoxiClient", constructors.Length);

                object? doxiClient = null;
                Exception? lastException = null;

                // Try: (string baseUrl, string username, string password)
                foreach (var ctor in constructors)
                {
                    var parameters = ctor.GetParameters();
                    if (parameters.Length == 3 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(string) &&
                        parameters[2].ParameterType == typeof(string))
                    {
                        try
                        {
                            doxiClient = ctor.Invoke(new object[] { apiBaseUrl, username, password });
                            if (doxiClient != null)
                            {
                                TrySetTenant(doxiClient, tenant);
                                _logger.LogInformation("Successfully created DoxiClient using constructor(string, string, string)");
                                return doxiClient;
                            }
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            _logger.LogDebug(ex, "Failed to create DoxiClient with constructor(string, string, string)");
                        }
                    }
                }

                // Try: (string baseUrl)
                if (doxiClient == null)
                {
                    foreach (var ctor in constructors)
                    {
                        var parameters = ctor.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                        {
                            try
                            {
                                doxiClient = ctor.Invoke(new object[] { apiBaseUrl });
                                if (doxiClient != null)
                                {
                                    await TrySetAuthenticationAsync(doxiClient, username, password, tenant);
                                    _logger.LogInformation("Successfully created DoxiClient using constructor(string)");
                                    return doxiClient;
                                }
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                _logger.LogDebug(ex, "Failed to create DoxiClient with constructor(string)");
                            }
                        }
                    }
                }

                // Try: () - parameterless
                if (doxiClient == null)
                {
                    foreach (var ctor in constructors)
                    {
                        if (ctor.GetParameters().Length == 0)
                        {
                            try
                            {
                                doxiClient = ctor.Invoke(Array.Empty<object>());
                                if (doxiClient != null)
                                {
                                    TrySetProperty(doxiClient, "BaseUrl", apiBaseUrl);
                                    TrySetProperty(doxiClient, "BaseAddress", apiBaseUrl);
                                    await TrySetAuthenticationAsync(doxiClient, username, password, tenant);
                                    _logger.LogInformation("Successfully created DoxiClient using parameterless constructor");
                                    return doxiClient;
                                }
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                _logger.LogDebug(ex, "Failed to create DoxiClient with parameterless constructor");
                            }
                        }
                    }
                }

                // If all constructors failed, return placeholder
                _logger.LogWarning("Failed to create DoxiClient instance using any constructor. Last error: {Error}", lastException?.Message);
                return new object(); // Placeholder - HTTP calls will be used instead
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while creating DoxiClient");
                return new object(); // Placeholder - HTTP calls will be used instead
            }
        }

        private async Task TrySetAuthenticationAsync(object client, string username, string password, string tenant)
        {
            var type = client.GetType();

            // Try setting tenant
            TrySetTenant(client, tenant);

            // Try calling Authenticate method
            var authMethod = type.GetMethod("Authenticate", new[] { typeof(string), typeof(string) });
            if (authMethod != null)
            {
                try
                {
                    var result = authMethod.Invoke(client, new object[] { username, password });
                    if (result is Task task)
                    {
                        await task;
                    }
                    _logger.LogDebug("Successfully called Authenticate method");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to call Authenticate method");
                }
            }

            // Try calling SetToken method (if we have a token, but we don't here)
            // Try setting properties
            TrySetProperty(client, "Username", username);
            TrySetProperty(client, "Password", password);
        }

        private void TrySetTenant(object client, string tenant)
        {
            TrySetProperty(client, "Tenant", tenant);
            TrySetProperty(client, "TenantId", tenant);

            var setTenantMethod = client.GetType().GetMethod("SetTenant", new[] { typeof(string) });
            if (setTenantMethod != null)
            {
                try
                {
                    setTenantMethod.Invoke(client, new object[] { tenant });
                    _logger.LogDebug("Successfully set tenant via SetTenant method");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to set tenant via SetTenant method");
                }
            }
        }

        private void TrySetProperty(object obj, string propertyName, object? value)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(obj, value);
                    _logger.LogDebug("Successfully set property {PropertyName}", propertyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to set property {PropertyName}: {Error}", propertyName, ex.Message);
            }
        }
    }
}

