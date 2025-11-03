using Microsoft.AspNetCore.Mvc;

namespace Consist.ProjectName.Filters
{
    public class ErrorResponse : ProblemDetails
    {
        public string? TraceId { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; }
    }
}
