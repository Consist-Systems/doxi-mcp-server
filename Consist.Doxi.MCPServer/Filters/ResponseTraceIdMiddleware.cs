using Consist.Exceptions;

namespace Consist.ProjectName.Filters
{
    public class ResponseTraceIdMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseTraceIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate a new UUID
            var requestId = Guid.NewGuid().ToString();

            // Add the UUID to the request headers
            context.Request.Headers.Add("traceId", requestId);
            // Add the UUID to the response headers as well (optional)
            context.Response.OnStarting(() => {
                context.Response.Headers.Add("traceId", requestId);
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

}
