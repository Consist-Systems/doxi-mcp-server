using Consist.Doxi.MCPServer.Domain;
using Consist.Doxi.MCPServer.Mapper;
using Consist.MCPServer.DoxiAPIClient;
using Consist.ProjectName.Filters;
using NLog;
using NLog.Web;

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

            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MapperRegistration>();
            });

            services.AddServiceDomain()
            .AddDoxiAPIClient(options =>
            {
                options.IdpURL = Configuration["DoxiAPIClient:IdpURL"];
                options.DoxiAPIUrl = Configuration["DoxiAPIClient:DoxiAPIUrl"];
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(ErrorHandlingFilterAttribute));
                options.Filters.Add(typeof(RequestResponseLogAttribute));
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

            app.UseMiddleware<ResponseTimeMiddleware>();
            app.UseMiddleware<ResponseTraceIdMiddleware>();

            app.UseEndpoints(endpoints =>
            {
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