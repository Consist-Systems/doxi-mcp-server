using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Consist.MCPServer.DoxiAPIClient
{
    public static class DIRegistration
    {
        public static IServiceCollection AddDoxiAPIClient(this IServiceCollection services, Action<DoxiAPIClientConfiguration> config)
        {
            services.Configure(config);
            services.AddSingleton<IDoxiClientService, DoxiClientService>();
            return services;
        }


    }
}
