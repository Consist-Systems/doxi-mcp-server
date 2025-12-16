using Consist.Doxi.MCPServer.Domain.AILogic;
using Consist.PDFTools.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.IO;

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

        [McpServerTool(Name = "AddTexts"), Description(@"Adds text elements to a PDF document and returns the modified PDF. 
The inputFile parameter accepts:
- A file path (e.g., 'C:\path\to\file.pdf' or './document.pdf')
- A file URI (e.g., 'file:///path/to/file.pdf')
- A base64-encoded string (for backward compatibility)
The prompt parameter describes what text should be added to the document.")]
        public async Task<DataContent> AddTexts(string prompt, string inputFile)
        {
            byte[] pdfFile = await ReadFileAsBytes(inputFile);
            var result = await DocumentEditorLogic.AddTexts(pdfFile, prompt);
            return new DataContent(result, "application/pdf");
        }

        private async Task<byte[]> ReadFileAsBytes(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Input file cannot be null or empty", nameof(input));
            }

            // Try to read as file path or URI first
            string? filePath = null;
            
            // Handle file:// URIs
            if (input.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    filePath = new Uri(input).LocalPath;
                }
                catch (UriFormatException)
                {
                    // Invalid URI format, will try other methods
                }
            }
            
            // Try direct file existence check first
            if (filePath == null && File.Exists(input))
            {
                filePath = input;
            }
            
            // Try with full path resolution (for relative paths)
            if (filePath == null)
            {
                try
                {
                    string fullPath = Path.GetFullPath(input);
                    if (File.Exists(fullPath))
                    {
                        filePath = fullPath;
                    }
                }
                catch
                {
                    // Path resolution failed, will try base64
                }
            }

            // If we found a valid file path, read it
            if (filePath != null && File.Exists(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }

            // Otherwise, try to decode as base64 (for backward compatibility)
            try
            {
                return Convert.FromBase64String(input);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException(
                    $"Invalid file input. Expected a valid file path, file URI, or base64-encoded string. " +
                    $"Attempted file path: {filePath ?? input}. " +
                    $"Base64 decode also failed: {ex.Message}", 
                    nameof(input), ex);
            }
        }
    }
}

