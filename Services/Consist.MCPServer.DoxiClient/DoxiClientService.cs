using Microsoft.Extensions.Logging;
using Doxi.APIClient;
using SimpleCache;
using Microsoft.Extensions.Options;

namespace Consist.MCPServer.DoxiAPIClient
{
    public interface IDoxiClientService
    {
        public DoxiClient this[DoxiClientContext key] { get; }
    }

    public class DoxiClientService : Dictionary<DoxiClientContext, DoxiClient>, IDoxiClientService
    {
        private readonly ILogger<DoxiClientService> _logger;
        private readonly DoxiAPIClientConfiguration _doxiAPIClientConfiguration;
        private readonly CacheDictionary<DoxiClientContext, DoxiClient> _doxiClients;

        public DoxiClientService(ILogger<DoxiClientService> logger,
            IOptions<DoxiAPIClientConfiguration> doxiAPIClientConfiguration)
        {
            _logger = logger;
            _doxiAPIClientConfiguration = doxiAPIClientConfiguration.Value;
            _doxiClients = new CacheDictionary<DoxiClientContext, DoxiClient>(GetDoxiClient);
        }

        private DoxiClient GetDoxiClient(DoxiClientContext doxiClientKey)
        {
            return new DoxiClient(_doxiAPIClientConfiguration.IdpURL,
                _doxiAPIClientConfiguration.DoxiAPIUrl,
                doxiClientKey.Tenant,
                doxiClientKey.Username,
                doxiClientKey.Password);
        }

        public new DoxiClient this[DoxiClientContext key]
        {
            get
            {
                return _doxiClients[key];
            }
        }
    }
}
