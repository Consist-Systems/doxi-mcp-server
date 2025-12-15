using Consist.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Consist.ProjectName.Filters
{
    public class RequestResponseLogAttribute : ActionFilterAttribute
    {
        private const string ACTION_SOURCE_FORMAT = "{0}.{1}";
        private const string ACTION_ARGUMENTS_FORMAT = "{0}: {1}";
        private const string ENTER_LOG_FORMAT = "------API Request CALL: {0}, Request: {1}";
        private const string EXIT_LOG_FORMAT = "------API Response CALL: {0}, Response: {1}";
        private readonly ILogger<RequestResponseLogAttribute> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<int, DateTime> _traceLog;
        private const string DURATION_LOG_PARM = "duration";

        private const string IP_PARM = "ip_address";

        private static readonly List<Type> contentTypes = new List<Type>
        {
            typeof(ContentResult),
            typeof(StatusCodeResult),
            typeof(ViewResult),
        };
        private static readonly List<Type> noContentTypes = new List<Type>
        {
            typeof(EmptyResult),
            typeof(FileResult),
            typeof(FileStreamResult)
        };

        public RequestResponseLogAttribute(ILogger<RequestResponseLogAttribute> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _traceLog = new Dictionary<int, DateTime>();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var requestId = context.HttpContext?.Request.GetHashCode();
            if (requestId.HasValue
                && !_traceLog.ContainsKey(requestId.Value))
            {
                _traceLog.Add(requestId.Value, DateTime.UtcNow);
            }

            var requestArguments = context.ActionArguments.Select(args =>
                string.Format(ACTION_ARGUMENTS_FORMAT,
                args.Key,
                JsonConvert.SerializeObject(args.Value))).ToArray();

            try
            {
                var actionSource = GetActionSource(context.RouteData);
                _logger.LogInformation(string.Format(ENTER_LOG_FORMAT,
                    actionSource,
                    string.Join(Environment.NewLine, requestArguments)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            dynamic content = context.Result;
            var response = "null";

            if (contentTypes.Contains(content.GetType()))
                response = JsonConvert.SerializeObject(content);
            else if (!noContentTypes.Contains(content.GetType()) && content.ContentType != "application/pdf")
                response = JsonConvert.SerializeObject(content.Value);

            var requestId = context.HttpContext?.Request.GetHashCode();
            if (requestId.HasValue
                && _traceLog.ContainsKey(requestId.Value))
            {
                var duration = (DateTime.UtcNow - _traceLog[requestId.Value]).TotalMilliseconds;
                _logger.SetPrameter(DURATION_LOG_PARM, duration.ToString());
            }

            var actionSource = GetActionSource(context.RouteData);
            
            _logger.LogInformation(string.Format(EXIT_LOG_FORMAT,
                 actionSource,
                 response));

        }

        private string GetActionSource(RouteData routeData)
        {
            return string.Format(ACTION_SOURCE_FORMAT, routeData.Values["Controller"], routeData.Values["Action"]);
        }

       
    }
}
