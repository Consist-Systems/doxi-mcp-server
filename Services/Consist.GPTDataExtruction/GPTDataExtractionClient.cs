using Consist.Doxi.Domain.Models;
using Consist.GPTDataExtruction.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Consist.GPTDataExtruction
{
public class GPTDataExtractionClient : IAIModelDataExtractionClient
{
        private readonly IServiceProvider _serviceProvider;

        private TemplateExtractorFromPDF GptTemplateExtractionService => _serviceProvider.GetService<TemplateExtractorFromPDF>();
        private TextElementsExtraction TextElementsExtraction => _serviceProvider.GetService<TextElementsExtraction>();

        private ImageDataExtructor ImageFieldsInformationExtractor => _serviceProvider.GetService<ImageDataExtructor>();
        private GenericGptClient GenericGptClient => _serviceProvider.GetService<GenericGptClient>();

        public GPTDataExtractionClient(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<TemplateInfoFromPDFwithFields> ExtractTemplateInformationFromPDF(IEnumerable<byte[]> documentPagesAsImages)
        {
            return GptTemplateExtractionService.ExtractAsync(documentPagesAsImages);
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

        public  Task<IEnumerable<TextElement>> GetTextElements(IEnumerable<byte[]> documentPagesAsImages, string documentFields, string documentStructure, string prompt)
        {
            return TextElementsExtraction.GetTextElements(documentPagesAsImages, documentFields, documentStructure, prompt);
        }
    }
}