using Consist.Doxi.Domain.Models;
using Consist.GPTDataExtruction.Model;

namespace Consist.GPTDataExtruction
{
    public class ImageDataExtructor
    {
        private readonly GenericGptClient _genericGptClient;

        public ImageDataExtructor(GenericGptClient genericGptClient)
        {
            _genericGptClient = genericGptClient;
        }

        internal async Task<FieldsPredictions> GetFieldPredictionByImageFiledsNumbers(IEnumerable<ExTemplatFlowElement> documentFields, TemplateInfoFromPDFwithFields templateInformationFromPDF, List<byte[]> labeledImages)
        {
            var fieldMap = new Dictionary<int, ExTemplatFlowElement>();
            string prompt;
            if (templateInformationFromPDF.Signers.Length > 1)
                prompt = @$"I have marked fields with red rectangles and numbers.
For each number, please provide a unique label and the signer from the following list:
{string.Join(", ", templateInformationFromPDF.Signers.Select(s => s.Title))}.
Field can only assigned to one signer. Return JSON with list of {{ FieldNumber, Label, Signer }}";
            else
                prompt = $@"I have marked fields with red rectangles and numbers.
For each number, please provide a unique labe. 
Return JSON with list of {{ FieldNumber, Label, Signer=""{templateInformationFromPDF.Signers.First().Title}"" }}";

            _genericGptClient.SetModelData(model: "gpt-4o");
            var gptResponse = await _genericGptClient.RunModelByFiles<FieldsPredictions>(labeledImages, prompt);

            return gptResponse;
        }
            
    }
}
