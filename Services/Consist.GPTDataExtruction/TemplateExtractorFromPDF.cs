using System.Text.Json;
using Consist.GPTDataExtruction.Model;
using Consist.PDFConverter;
using Microsoft.Extensions.Logging;

namespace Consist.GPTDataExtruction
{
    public class TemplateExtractorFromPDF
    {
        #region INSTRUCTIONS

        // Phase 1: extract structure only (no fields)
        private const string STRUCTURE_INSTRUCTION = @"
Analyze the document and extract the template structure.

TemplateName = the main title of the form (top header or boldest text).

SendMethodType: Use these values:
- 0 = QueuedFlow - siner one sign then the next
- 1 = ParallelFlow - all signers sign at the same time - must have multiple signers

Languages = detect ALL languages appearing in the document.
Return a space-separated list of language codes (ISO 639-1). Example: ""he en"".
If multiple languages appear in text on any page, include all of them.
Do not return words, only language codes.

Identify ALL signers in the document. A signer = any role that must review, approve, sign, or complete part of the form.

For each signer:
- Title: role name as shown in the document
- SignerType: Use these enum values:
  * 0 = Changeable - can be assigned to any user at sending time
  * 1 = Static - must be assigned to a specific user
  * 2 = Anonymous - the signer enter url to sign without identification
at least one signer must be set

Do NOT include any field labels in this response.
Use all pages together.";

        // Phase 2: extract fields for a specific signer
        private const string FIELDS_INSTRUCTION_TEMPLATE = @"
Analyze the document for the specific signer role: ""{0}""

List ALL field labels this signer must complete in the document.
A field label = any titled element that requires this signer to provide input, write, sign, select, approve, or respond.

Include (for this signer only):
- text fields (single or multi-line)
- signature areas
- date/number fields
- approval/review fields
- group titles for selections or checkboxes
- section headers that correspond to required input

Do NOT include:
- option values
- checkbox/radio items
- descriptive text

Use all pages together.";

        #endregion

        private readonly ILogger<TemplateExtractorFromPDF> _logger;
        private readonly GPTDataExtructionConfiguration _config;
        private readonly IDocumentConverter _documentConverter;
        private readonly GenericGptClient _gptClient;

        public TemplateExtractorFromPDF(
            ILogger<TemplateExtractorFromPDF> logger,
            GPTDataExtructionConfiguration config,
            IDocumentConverter documentConverter,
            GenericGptClient gptClient)
        {
            _logger = logger;
            _config = config;
            _documentConverter = documentConverter;
            _gptClient = gptClient;
        }

        public async Task<TemplateInfoFromPDFwithFields> ExtractAsync(byte[] pdfBytes)
        {
            var images = (await _documentConverter.PDFToImages(pdfBytes)).ToList();
            if (!images.Any())
                throw new Exception("PDF contains no pages or failed to convert to images.");

            // Phase 1: extract template structure (no fields)
            var structure = await ExtractStructureAsync(images);

            // Convert to TemplateInfoFromPDFwithFields
            var result = new TemplateInfoFromPDFwithFields
            {
                TemplateName = structure.TemplateName ?? string.Empty,
                SendMethodType = structure.SendMethodType,
                Languages = structure.Languages ?? string.Empty,
                Signers = structure.Signers?
                    .Select(s => new SignerResponseWithFields
                    {
                        Title = s.Title ?? string.Empty,
                        SignerType = s.SignerType,
                        Fields = Array.Empty<string>()
                    })
                    .ToArray()
                    ?? Array.Empty<SignerResponseWithFields>()
            };

            // Phase 2: for each signer, extract fields for that signer
            foreach (var signer in result.Signers)
            {
                signer.Fields = await ExtractFieldsForSignerAsync(images, signer.Title);
            }

            _logger.LogDebug("Final TemplateInfoFromPDFwithFields: " +
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

            return result;
        }

        #region Phase 1: Structure

        private async Task<TemplateInfoFromPDF> ExtractStructureAsync(List<byte[]> images)
        {
            var batches = Batch(images, _config.TemplateExtractorFromPDFConfig.MaxPagesPerBatch);

            var final = new TemplateInfoFromPDF
            {
                TemplateName = string.Empty,
                SendMethodType = null,
                Languages = string.Empty,
                Signers = Array.Empty<SignerResponse>()
            };
            var finalSigners = new List<SignerResponse>();

            foreach (var batch in batches)
            {
                var batchResult = await _gptClient.RunModelByFiles<TemplateInfoFromPDF>(
                    batch,
                    STRUCTURE_INSTRUCTION);

                // Merge results
                if (string.IsNullOrWhiteSpace(final.TemplateName) &&
                    !string.IsNullOrWhiteSpace(batchResult.TemplateName))
                {
                    final.TemplateName = batchResult.TemplateName;
                }

                if (!final.SendMethodType.HasValue && batchResult.SendMethodType.HasValue)
                {
                    final.SendMethodType = batchResult.SendMethodType;
                }

                if (string.IsNullOrWhiteSpace(final.Languages) &&
                    !string.IsNullOrWhiteSpace(batchResult.Languages))
                {
                    final.Languages = batchResult.Languages;
                }

                foreach (var signer in batchResult.Signers ?? Array.Empty<SignerResponse>())
                {
                    var existing = final.Signers.FirstOrDefault(s =>
                        string.Equals(s.Title, signer.Title, StringComparison.OrdinalIgnoreCase) &&
                        s.SignerType == signer.SignerType);

                    if (existing == null)
                    {
                        finalSigners.Add(new SignerResponse
                        {
                            Title = signer.Title ?? string.Empty,
                            SignerType = signer.SignerType
                        });
                    }
                }
            }
            final.Signers = finalSigners.ToArray();
            final.TemplateName ??= string.Empty;
            final.Signers ??= new SignerResponse[0];

            return final;
        }

        #endregion

        #region Phase 2: Fields per signer

        private async Task<string[]> ExtractFieldsForSignerAsync(List<byte[]> images, string signerTitle)
        {
            var batches = Batch(images, _config.TemplateExtractorFromPDFConfig.MaxPagesPerBatch);
            var fields = new HashSet<string>();

            foreach (var batch in batches)
            {
                var instruction = string.Format(FIELDS_INSTRUCTION_TEMPLATE, signerTitle);

                var dto = await _gptClient.RunModelByFiles<SignerFieldsDto>(
                    batch,
                    instruction,
                    "image/png");

                if (dto?.Fields == null)
                    continue;

                foreach (var f in dto.Fields)
                {
                    if (!string.IsNullOrWhiteSpace(f))
                        fields.Add(f.Trim());
                }
            }

            return fields.ToArray();
        }

        private class SignerFieldsDto
        {
            public List<string> Fields { get; set; }
        }

        #endregion

        #region Helpers

        private List<List<byte[]>> Batch(List<byte[]> images, int batchSize)
        {
            var result = new List<List<byte[]>>();
            for (int i = 0; i < images.Count; i += batchSize)
                result.Add(images.Skip(i).Take(batchSize).ToList());
            return result;
        }

        #endregion
    }
}