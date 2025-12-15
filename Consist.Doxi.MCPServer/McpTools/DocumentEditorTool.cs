using Consist.Doxi.MCPServer.Domain.AILogic;
using Consist.PDFTools.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Consist.ProjectName.McpTools
{
    [McpServerToolType]
    public class DocumentEditorTool
    {
        private readonly IServiceProvider _serviceProvider;
        private DocumentEditorLogic DocumentEditorLogic => _serviceProvider.GetRequiredService<DocumentEditorLogic>();

        public DocumentEditorTool(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [McpServerTool(Name = "AddTexts"), Description("Adds text elements to a PDF document and returns the modified PDF. Accepts PDF file as base64-encoded string and text elements as JSON array.")]
        public async Task<DataContent> AddTexts(string pdfFileBase64, string prompt)
        {
            
            byte[] pdfFile;
            try
            {
                pdfFile = Convert.FromBase64String(pdfFileBase64);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid base64 PDF file", nameof(pdfFileBase64), ex);
            }
            
            var result = await DocumentEditorLogic.AddTexts(pdfFile, prompt);
            return new DataContent(result, "application/pdf");
        }
    }
}

