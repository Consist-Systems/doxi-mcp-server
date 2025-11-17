using Consist.GPTDataExtruction.Model;

namespace Consist.GPTDataExtruction
{
    public interface IGPTDataExtractionClient
    {
        Task<IEnumerable<FieldWithSigner>> ExtractFieldLableToSignerMapping(IEnumerable<SignerInfo> signers, IEnumerable<FieldWithPage> fieldsWithPage, byte[] document);
        Task<CreateTemplateInformation> ExtractTemplateInformation(string templateInstructions);
    }
}
