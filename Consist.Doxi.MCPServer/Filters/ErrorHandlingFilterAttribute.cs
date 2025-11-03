using Consist.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Net;

namespace Consist.ProjectName.Filters
{
    public class ErrorHandlingFilterAttribute : ExceptionFilterAttribute
    {
        private readonly ILogger<ErrorHandlingFilterAttribute> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _isDevelopment;
        private const string IP_PARM = "ip_address";

        public ErrorHandlingFilterAttribute(ILogger<ErrorHandlingFilterAttribute> logger,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _isDevelopment = env.IsDevelopment();
        }

        public override void OnException(ExceptionContext context)
        {
            _logger.SetPrameter(IP_PARM, GetUserIp(context.HttpContext));

            context.HttpContext.Response.ContentType = "text/html";
            var exception = context.Exception.InnerException ?? context.Exception;
            context.Result = new JsonResult(exception.Message);

            if (exception is ArgumentException ||
                context.Exception.Source == "Newtonsoft.Json" ||
                exception is FormatException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else if (exception is UnauthorizedAccessException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else if (exception is StatusCodeException)
            {
                var statusCodeException = (StatusCodeException)exception;
                context.HttpContext.Response.StatusCode = statusCodeException.HttpErrorStatusCode;
            }
            else
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            var error = new Dictionary<string, List<string>>();
            if (context.HttpContext.Response.StatusCode != (int)HttpStatusCode.InternalServerError
                || _isDevelopment)
            {
                error.Add(exception.Source, new List<string>(new[] { exception.ToString() }));
            }
            else
            {
                error.Add(exception.Source, new List<string>(new[] { "Internal Error" }));
            }

            context.Result = new JsonResult(new ErrorResponse
            {
                Type = context.HttpContext.Response.StatusCode.ToString(),
                TraceId = context.HttpContext.Request.Headers["traceId"],
                Status = (int)context.HttpContext.Response.StatusCode,
                Title = "Errors occurred.",
                Errors = error
            }) ;

            if (context.HttpContext.Response.StatusCode >= 500 && context.HttpContext.Response.StatusCode < 600)
                _logger.LogError(context.Exception.ToString());
            else
                _logger.LogWarning(context.Exception.ToString());

        }

        public string GetUserIp(HttpContext httpContext)
        {
            try
            {
                var xForwardedForHeader = httpContext.Request.Headers?.FirstOrDefault(x => x.Key == _configuration["ClientIPHeader"]);
                if (xForwardedForHeader != null
                    && !string.IsNullOrEmpty(httpContext.Request.Headers[_configuration["ClientIPHeader"]]))
                {
                    return httpContext.Request.Headers[_configuration["ClientIPHeader"]];
                }

                var userIp = httpContext.Connection?.RemoteIpAddress?.MapToIPv4()?.ToString();

                if (string.IsNullOrEmpty(userIp))
                {
                    var headersStr = string.Empty;
                    if (httpContext.Request.Headers != null)
                    {
                        headersStr = JsonConvert.SerializeObject(httpContext.Request.Headers);
                    }
                }
                return userIp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return null;
            }
        }

    }


}
