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

                // Configure URLs - listen on all interfaces for Docker compatibility
                // Only override URLs when running in Docker or when ASPNETCORE_HTTP_PORTS is explicitly set
                var httpPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS");
                var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                if (!string.IsNullOrEmpty(httpPort) || isDocker)
                {
                    var port = httpPort ?? "5000";
                    var listenAddress = isDocker ? "0.0.0.0" : "localhost";
                    builder.UseUrls($"http://{listenAddress}:{port}");
                }

                // Configure Kestrel to use only TLS 1.2 and 1.3
                // Only configure explicit listener when in Docker
                if (isDocker && !string.IsNullOrEmpty(httpPort))
                {
                    builder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, int.Parse(httpPort), listenOptions =>
                        {
                            // HTTP listener
                        });
                        options.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                        });
                    });
                }
                else
                {
                    builder.ConfigureKestrel(options =>
                    {
                        options.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                        });
                    });
                }

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
