using Microsoft.AspNetCore;
using NLog;
using NLog.Web;
using System.Net;
using System.Security.Authentication;

namespace Consist.ProjectName
{
    public class Program
    {
        private static NLog.ILogger _logger;


        public static void Main(string[] args)
        {
            _logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
            try
            {
                _logger.Debug("init main");

                var webhost = CreateHostBuilder(args).Build();
                webhost.Run();
            }
            catch (Exception ex)
            {
                //NLog: catch setup errors
                _logger.Fatal(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args)
        {
            try
            {
                _logger.Debug("Enter CreateHostBuilder");
                var builder = WebHost.CreateDefaultBuilder(args);

                // Configure Kestrel to use only TLS 1.2 and 1.3
                builder.ConfigureKestrel(options =>
                {
                    options.ConfigureHttpsDefaults(httpsOptions =>
                    {
                        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                    });
                });

                builder = builder.UseStartup<Startup>();
                builder = builder.UseDefaultServiceProvider(options =>
                        options.ValidateScopes = false);
                builder = builder
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    });
                
                builder = builder
                   .UseNLog();

                _logger.Debug("Exit CreateHostBuilder");
                return builder;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }
    }
}
