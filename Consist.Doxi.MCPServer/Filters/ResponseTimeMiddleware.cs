namespace Consist.ProjectName.Filters
{
    public class ResponseTimeMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseTimeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            context.Response.OnStarting(() => {
                watch.Stop();
                var responseTime = watch.ElapsedMilliseconds;
                context.Response.Headers.Add("Retry-After", responseTime.ToString());
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

}
