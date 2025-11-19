using Consist.GPTDataExtruction.Model;

namespace Consist.GPTDataExtruction
{
    public interface IAIModelDataExtractionClient
    {
        Task<TemplateInfoFromPDFwithFields> ExtractTemplateInformationFromPDF(byte[] pdfFile);
        Task<CreateTemplateInformation> ExtractTemplateInformationFromPrompt(string templateInstructions);
    }
}
