using Consist.GPTDataExtruction.Model;

namespace Consist.GPTDataExtruction
{
    internal class GPTDataExtractionClient : IGPTDataExtractionClient
    {
        /// <summary>
        /// Use GPT to detect which field belongs to which signer
        /// </summary>
        /// <param name="signers"></param>
        /// <param name="fieldsWithPage"></param>
        /// <param name="document"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IEnumerable<FieldWithSigner>> ExtractFieldLableToSignerMapping(IEnumerable<SignerInfo> signers, IEnumerable<FieldWithPage> fieldsWithPage, byte[] document)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Call model ft:gpt-4.1-nano-2025-04-14:personal:doxicreatetemplateinformation:CcsWe2l4 to extract template information
        /// </summary>
        /// <param name="templateInstructions"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<CreateTemplateInformation> ExtractTemplateInformation(string templateInstructions)
        {
            throw new NotImplementedException();
        }
    }
}
