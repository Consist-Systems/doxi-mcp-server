using Microsoft.Extensions.DependencyInjection;

namespace Consist.Doxi.MCPServer.Domain
{
    public static class DIRegistration
    {
        public static IServiceCollection AddServiceDomain(this IServiceCollection services)
        {
            services.AddScoped<IContextInformation, ContextInformation>();
            services.AddScoped<DoxiAPIWrapper>();
            return services;
        }

        
    }
}
