using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Consist.GPTDataExtruction
{
    public static class DIRegistration
    {
        public static IServiceCollection AddGPTDataExtraction(this IServiceCollection services,Action<GPTDataExtructionConfiguration> config)
        {
            services.Configure(config);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<GPTDataExtructionConfiguration>>().Value);
            services.AddSingleton<IAIModelDataExtractionClient, GPTDataExtractionClient>();
            services.AddSingleton<TemplateExtractorFromPDF>();
            services.AddSingleton<TemplateExtractorFromText>();
            
            return services;
        }
    }
}

