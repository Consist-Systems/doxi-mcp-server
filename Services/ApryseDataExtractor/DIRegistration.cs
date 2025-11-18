using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApryseDataExtractor
{
    public static class DIRegistration
    {
        public static IServiceCollection AddApryseDataExtractor(this IServiceCollection services,Action<ApryseDataExtractorConfiguration> config)
        {
            services.Configure(config);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ApryseDataExtractorConfiguration>>().Value);
            services.AddSingleton<IDocumentFieldExtractor, DocumentFieldExtractor>();
            return services;
        }
    }
}

