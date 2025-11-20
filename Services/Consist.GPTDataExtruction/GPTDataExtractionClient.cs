using Consist.GPTDataExtruction.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Consist.GPTDataExtruction
{
public class GPTDataExtractionClient : IAIModelDataExtractionClient
{
        private readonly IServiceProvider _serviceProvider;

        private TemplateExtractorFromPDF GptTemplateExtractionService => _serviceProvider.GetService<TemplateExtractorFromPDF>();
        private GenericGptClient GenericGptClient => _serviceProvider.GetService<GenericGptClient>();

        public GPTDataExtractionClient(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<TemplateInfoFromPDFwithFields> ExtractTemplateInformationFromPDF(byte[] pdfFile)
        {
            return GptTemplateExtractionService.ExtractAsync(pdfFile);
        }

        public Task<CreateTemplateInformation> ExtractTemplateInformationFromPrompt(string templateInstructions)
        {
            return GenericGptClient.RunModelByText<CreateTemplateInformation>(templateInstructions);
        }
    }
}