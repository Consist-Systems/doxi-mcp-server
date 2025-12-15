using Consist.Doxi.Domain.Models;
using Consist.GPTDataExtruction.Model;

namespace Consist.GPTDataExtruction
{
    public interface IAIModelDataExtractionClient
    {
        Task<TemplateInfoFromPDFwithFields> ExtractTemplateInformationFromPDF(IEnumerable<byte[]> documentPagesAsImages);
        Task<CreateTemplateInformation> ExtractTemplateInformationFromPrompt(string templateInstructions);

        Task<FieldsPredictions> GetFieldsPredictionsFromImages(IEnumerable<ExTemplatFlowElement> documentFields, TemplateInfoFromPDFwithFields templateInformationFromPDF, List<byte[]> labeledImages);
        Task<IEnumerable<TextElement>> GetTextElements(IEnumerable<byte[]> documentPagesAsImages, string documentFields, string documentStructure, string prompt);
    }
}
