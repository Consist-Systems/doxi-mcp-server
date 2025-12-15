using ApryseDataExtractor;
using AutoMapper;
using Consist.GPTDataExtruction;
using Consist.PDFTools;
using Consist.PDFTools.Model;
using Newtonsoft.Json;

namespace Consist.Doxi.MCPServer.Domain.AILogic
{
    public class DocumentEditorLogic
    {
        private readonly IPDFEditor _pdfEditor;
        private readonly IAIModelDataExtractionClient _aIModelDataExtractionClient;
        private readonly IDocumentConverter _documentConverter;
        private readonly IDocumentFieldExtractor _documentFieldExtractor;
        private readonly IMapper _mapper;

        public DocumentEditorLogic(IPDFEditor pdfEditor,
            IAIModelDataExtractionClient _aIModelDataExtractionClient,
            IDocumentConverter documentConverter,
            IDocumentFieldExtractor documentFieldExtractor,
            IMapper mapper)
        {
            _pdfEditor = pdfEditor;
            this._aIModelDataExtractionClient = _aIModelDataExtractionClient;
            _documentConverter = documentConverter;
            _documentFieldExtractor = documentFieldExtractor;
            _mapper = mapper;
        }

        public async Task<byte[]> AddTexts(byte[] pdfFile, string prompt)
        {
            //where doking reference (reference to document element)
            //get document fields and metadata
            var documentFields = JsonConvert.SerializeObject(await _documentFieldExtractor.GetDocumentFields(pdfFile, null));
            var documentStructure = JsonConvert.SerializeObject(await _documentFieldExtractor.GetDocumentStructure(pdfFile, null));
            //get docking point
            //get the fonts data
            //get the text position
            var documentPagesAsImages = await _documentConverter.PDFToImages(pdfFile);
            var textElementsAI = await _aIModelDataExtractionClient.GetTextElements(documentPagesAsImages, documentFields, documentStructure, prompt);

            //add the text to PDF
            var textElements = _mapper.Map<IEnumerable<TextElement>>(textElementsAI);
            return await _pdfEditor.AddTexts(pdfFile, textElements);
        }
    }
}

