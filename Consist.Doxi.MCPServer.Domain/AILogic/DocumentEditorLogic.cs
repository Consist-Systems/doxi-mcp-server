using Consist.PDFTools;
using Consist.PDFTools.Model;

namespace Consist.Doxi.MCPServer.Domain.AILogic
{
    public class DocumentEditorLogic
    {
        private readonly IPDFEditor _pdfEditor;

        public DocumentEditorLogic(IPDFEditor pdfEditor)
        {
            _pdfEditor = pdfEditor;
        }

        public async Task<byte[]> AddTexts(byte[] pdfFile, IEnumerable<TextElement> textElements)
        {
            return await _pdfEditor.AddTexts(pdfFile, textElements);
        }
    }
}

