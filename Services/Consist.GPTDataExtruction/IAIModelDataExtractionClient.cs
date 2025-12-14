using Consist.Doxi.Domain.Models;
using Consist.GPTDataExtruction.Model;

namespace Consist.GPTDataExtruction
{
    public interface IAIModelDataExtractionClient
    {
        Task<TemplateInfoFromPDFwithFields> ExtractTemplateInformationFromPDF(byte[] pdfFile);
        Task<CreateTemplateInformation> ExtractTemplateInformationFromPrompt(string templateInstructions);

        Task<FieldsPredictions> GetFieldsPredictionsFromImages(IEnumerable<ExTemplatFlowElement> documentFields, TemplateInfoFromPDFwithFields templateInformationFromPDF, List<byte[]> labeledImages);
    }
}
