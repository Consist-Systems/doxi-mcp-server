using Consist.GPTDataExtruction.Model;

namespace Consist.GPTDataExtruction
{
    public interface IAIModelDataExtractionClient
    {
        Task<TemplateInfoFromPDF> ExtractTemplateInformationFromPDF(byte[] pdfFile);
        Task<CreateTemplateInformation> ExtractTemplateInformationFromPrompt(string templateInstructions);
    }
}
