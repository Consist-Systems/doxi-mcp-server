using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Consist.Doxi.MCPServer.Domain
{
    public interface IContextInformation
    {
        string Tenant { get; }
    }

    public class ContextInformation : IContextInformation
    {
        public ContextInformation(IHttpContextAccessor httpContextAccessor)
        {
            Tenant = httpContextAccessor.HttpContext?.GetRouteValue("tenant")?.ToString() ?? string.Empty;
        }
        public string Tenant { get; private set; }
    }
}
