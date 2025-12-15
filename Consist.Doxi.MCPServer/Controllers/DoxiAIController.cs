using Consist.Doxi.MCPServer.Domain.AILogic;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace Consist.Doxi.MCPServer.Controllers
{
    [ApiController]
    public class DoxiAIController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private TemplateLogic TemplateLogic => _serviceProvider.GetService<TemplateLogic>();
        private DocumentEditorLogic DocumentEditorLogic => _serviceProvider.GetService<DocumentEditorLogic>();
        
        public DoxiAIController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Create Doxi template from PDF file. The templateFile parameter is the PDF file needed to create the template. 
        /// The templateInstructions parameter provides information on creating the template.
        /// </summary>
        /// <param name="tenant">Tenant name from the route (automatically populated by ContextInformation)</param>
        /// <param name="username">Doxi API username</param>
        /// <param name="password">Doxi API password</param>
        /// <param name="templateInstructions">Instructions describing the template to be created</param>
        /// <param name="templateFile">The template document (PDF/Word/Image)</param>
        /// <returns>AddTemplateResponse with TemplateId</returns>
        [HttpPost("{tenant}/v6/template")]
        public async Task<IActionResult> AddTemplate(
            [FromRoute] string tenant,
            [FromForm][Required] string username,
            [FromForm][Required] string password,
            [FromForm] string? templateInstructions,
            [FromForm][Required] IFormFile templateFile)
        {
            byte[] templateDocument;
            using (var memoryStream = new MemoryStream())
            {
                await templateFile.CopyToAsync(memoryStream);
                templateDocument = memoryStream.ToArray();
            }

            var result = await TemplateLogic.AddTemplate(username, password, templateDocument, templateInstructions);
            
            return Ok(result);
        }

        [HttpPut("{tenant}/v6/pdf")]
        public async Task<IActionResult> SetTexts([FromForm] string? prompt,
            [FromForm][Required] IFormFile pdfFile)
        {
            byte[] pdfDocumentByte;
            using (var memoryStream = new MemoryStream())
            {
                await pdfFile.CopyToAsync(memoryStream);
                pdfDocumentByte = memoryStream.ToArray();
            }

            // Process PDF
           var resultPdfBytes = await DocumentEditorLogic.AddTexts(pdfDocumentByte, prompt);

            if (resultPdfBytes == null || resultPdfBytes.Length == 0)
                return StatusCode(StatusCodes.Status500InternalServerError, "PDF processing failed.");

            // Return PDF file
            return File(
                fileContents: resultPdfBytes,
                contentType: "application/pdf",
                fileDownloadName: "edited.pdf"
            );
        }
    }
}

