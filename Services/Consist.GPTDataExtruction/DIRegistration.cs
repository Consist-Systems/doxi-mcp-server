using Microsoft.Extensions.DependencyInjection;

namespace Consist.GPTDataExtruction
{
    public static class DIRegistration
    {
        public static IServiceCollection AddGPTDataExtraction(this IServiceCollection services)
        {
            services.AddScoped<IGPTDataExtractionClient, GPTDataExtractionClient>();
            return services;
        }
    }
}

