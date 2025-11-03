namespace Consist.ProjectName.Services
{
    /// <summary>
    /// Service interface for managing DoxiClient instances with caching
    /// </summary>
    public interface IDoxiClientService
    {
        /// <summary>
        /// Gets or creates a DoxiClient instance for the specified tenant and username.
        /// Returns cached instance if available (within 10 minutes), otherwise creates a new one.
        /// </summary>
        /// <param name="tenant">Tenant identifier</param>
        /// <param name="username">Username for authentication</param>
        /// <param name="password">Password for authentication</param>
        /// <param name="apiBaseUrl">Base URL for the Doxi API</param>
        /// <returns>DoxiClient instance</returns>
        Task<object> GetOrCreateClientAsync(string tenant, string username, string password, string apiBaseUrl);
    }
}

