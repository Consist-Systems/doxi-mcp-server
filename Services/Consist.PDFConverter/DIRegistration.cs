using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Syncfusion.Licensing;

namespace Consist.PDFConverter
{
    public static class DIRegistration
    {
        public static IServiceCollection AddPDFConverter(this IServiceCollection services, Action<PDFConverterConfiguration> config)
        {
            // Create a temporary configuration instance to get the license key
            var tempConfig = new PDFConverterConfiguration();
            config(tempConfig);
            
            // Register Syncfusion license immediately with key from configuration
            if (!string.IsNullOrWhiteSpace(tempConfig.LicenseKey))
            {
                SyncfusionLicenseProvider.RegisterLicense(tempConfig.LicenseKey);
            }
            
            // Configure the settings for dependency injection
            services.Configure(config);

            // Register configuration instance
            services.AddSingleton<IDocumentConverter, SyncfusionDocumentConverter>();
            
            return services;
        }
    }
}

