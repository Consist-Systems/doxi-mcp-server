using Consist.Doxi.Domain.Models;
using Consist.GPTDataExtruction.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Consist.GPTDataExtruction
{
public class GPTDataExtractionClient : IAIModelDataExtractionClient
{
        private readonly IServiceProvider _serviceProvider;

        private TemplateExtractorFromPDF GptTemplateExtractionService => _serviceProvider.GetService<TemplateExtractorFromPDF>();

        private ImageDataExtructor ImageFieldsInformationExtractor => _serviceProvider.GetService<ImageDataExtructor>();
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
            var prompt = @$"by the text that the user set, try to fill the JSON result. if no data leave the field null.
Text:{templateInstructions}";
            
            return GenericGptClient.RunModelByText<CreateTemplateInformation>(prompt);
        }

        public Task<FieldsPredictions> GetFieldsPredictionsFromImages(IEnumerable<ExTemplatFlowElement> documentFields, TemplateInfoFromPDFwithFields templateInformationFromPDF, List<byte[]> labeledImages)
        {
            return ImageFieldsInformationExtractor.GetFieldPredictionByImageFiledsNumbers(documentFields, templateInformationFromPDF, labeledImages);
        }
    }
}