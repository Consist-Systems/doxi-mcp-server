using ApryseDataExtractor;
using Consist.Doxi.MCPServer.Domain;
using Consist.GPTDataExtruction;
using Consist.MCPServer.DoxiAPIClient;
using Consist.PDFConverter;
using Consist.ProjectName.Filters;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using pdftron.PDF.OCG;

namespace Consist.ProjectName
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration,
            ILogger<Startup> logger)
        {
            Configuration = configuration;
            this._logger = logger;
        }

        public IConfiguration Configuration { get; }

        // Startup.cs (excerpt)
        public void ConfigureServices(IServiceCollection services)
        {
            LogManager.Setup().LoadConfigurationFromAppSettings();
            _logger.LogDebug("Enter ConfigureServices");

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                    var cors = Configuration["AllowedOrigins"];
                    if (!string.IsNullOrEmpty(cors))
                    {
                        _logger.LogInformation($"Add CORS to origens: {cors}");
                        var allowedOrigens = Configuration["AllowedOrigins"].Split(";");
                        builder.WithOrigins(allowedOrigens);
                    }
                });
            });

            services.AddHttpContextAccessor();

            services.AddHttpClient();

            

            services.AddServiceDomain()
            .AddDoxiAPIClient(config =>
            {
                Configuration.GetSection("DoxiAPIClient").Bind(config);
            })
            .AddGPTDataExtraction(config=>
            {
                Configuration.GetSection("GPTDataExtraction").Bind(config);
            })
            .AddApryseDataExtractor(config =>
            {
                config.ApryseApiKey = Configuration["ApryseApiKey"];
            })
            .AddPDFConverter(config =>
            {
                config.LicenseKey = Configuration["PDFConverter:LicenseKey"];
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(ErrorHandlingFilterAttribute));
                options.Filters.Add(typeof(RequestResponseLogAttribute));
            });

            // Add Swagger/OpenAPI support
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Doxi MCP Server API",
                    Version = "v1",
                    Description = "API for managing document signing flows via the Doxi Sign API"
                });
            });

            // MCP over HTTP, scanning this assembly for [McpServerTool] methods
            services.AddMcpServer()
                    .WithHttpTransport()
                    .WithToolsFromAssembly(typeof(Startup).Assembly);

            _logger.LogDebug("Exit ConfigureServices");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            _logger.LogDebug("Enter Configure");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors();

            // Enable Swagger middleware
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Doxi MCP Server API v1");
                c.RoutePrefix = "swagger";
            });

            app.UseMiddleware<ResponseTimeMiddleware>();
            app.UseMiddleware<ResponseTraceIdMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                // Map all API controllers
                endpoints.MapControllers();

                endpoints.MapMcp("/{tenant}/mcp");

                // Serve MCP metadata.json
                endpoints.MapGet("/metadata", async context =>
                {
                    var json = await File.ReadAllTextAsync("McpTools/metadata.json");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                });
            });

            _logger.LogDebug("Exit Configure");
        }
    }
}