using System.Text.Json;
using Consist.GPTDataExtruction.Model;
using Consist.PDFConverter;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace Consist.GPTDataExtruction
{
    public class TemplateExtractorFromPDF
    {
        #region INSTRUCTIONS

        // Phase 1: extract structure only (no fields)
        private const string STRUCTURE_INSTRUCTION = @"
Return ONLY valid JSON:
{""TemplateName"":"""",""SendMethodType"":0,""Languages"":"""",""Signers"":[{""Title"":"""",""SignerType"":0}]}

SendMethodType: 0=QueuedFlow,1=ParallelFlow
SignerType: 0=Changeable,1=Static,2=Anonymous

TemplateName = the main title of the form (top header or boldest text).

Languages = detect ALL languages appearing in the document.
Return a space-separated list of language codes (ISO 639-1). Example: ""he en"".
If multiple languages appear in text on any page, include all of them.
Do not return words, only language codes.

Identify ALL signers in the document. A signer = any role that must review, approve, sign, or complete part of the form.

For each signer return:
- Title: role name as shown in the document
- SignerType: enum value (0,1,2)

Do NOT include any field labels in this response.
Use all pages together.
JSON only, no extra text.
";



        // Phase 2: extract fields for a specific signer
        private const string FIELDS_INSTRUCTION_TEMPLATE = @"
Return ONLY valid JSON:
{{""Fields"":[]}}


Signer role: ""{0}""

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

Use all pages together.
JSON only, no extra text.
";


        #endregion

        private readonly ILogger<TemplateExtractorFromPDF> _logger;
        private readonly GPTDataExtructionConfiguration _config;
        private readonly IDocumentConverter _documentConverter;

        public TemplateExtractorFromPDF(
            ILogger<TemplateExtractorFromPDF> logger,
            GPTDataExtructionConfiguration config,
            IDocumentConverter documentConverter)
        {
            _logger = logger;
            _config = config;
            _documentConverter = documentConverter;
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
                        
                        Fields = new List<string>()
                    })
                    .ToList()
                    ?? new List<SignerResponseWithFields>()
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
                Signers = new List<SignerResponse>()
            };

            foreach (var batch in batches)
            {
                var batchResult = await CallGptForStructureBatch(batch);

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

                foreach (var signer in batchResult.Signers)
                {
                    var existing = final.Signers.FirstOrDefault(s =>
                        string.Equals(s.Title, signer.Title, StringComparison.OrdinalIgnoreCase) &&
                        s.SignerType == signer.SignerType);

                    if (existing == null)
                    {
                        final.Signers.Add(new SignerResponse
                        {
                            Title = signer.Title ?? string.Empty,
                            SignerType = signer.SignerType
                        });
                    }
                }
            }

            final.TemplateName ??= string.Empty;
            final.Signers ??= new List<SignerResponse>();

            return final;
        }

        private async Task<TemplateInfoFromPDF> CallGptForStructureBatch(List<byte[]> images)
        {
            try
            {
                var body = new
                {
                    model = _config.TemplateExtractorFromPDFConfig.Model,
                    messages = BuildStructureMessages(images),
                    response_format = new
                    {
                        type = "json_object"
                    }
                };

                string rawResponse = await _config.TemplateExtractorFromPDFConfig.Endpoint
                    .WithHeader("Authorization", $"Bearer {_config.GPTAPIKey}")
                    .WithHeader("Content-Type", "application/json")
                    .PostJsonAsync(body)
                    .ReceiveString();

                using var doc = JsonDocument.Parse(rawResponse);

                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception("GPT returned empty content for structure.");

                var parsed = JsonSerializer.Deserialize<TemplateInfoFromPDF>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed == null)
                    throw new Exception("Parsed GPT structure content is null.");

                parsed.TemplateName ??= string.Empty;
                parsed.Signers ??= new List<SignerResponse>();

                foreach (var signer in parsed.Signers)
                {
                    signer.Title ??= string.Empty;
                }

                return parsed;
            }
            catch (FlurlHttpException httpEx)
            {
                string errorBody = await httpEx.GetResponseStringAsync();
                throw new Exception($"GPT API (structure) returned HTTP {(int?)httpEx.StatusCode}: {errorBody}");
            }
            catch (Exception ex)
            {
                throw new Exception($"GPT structure processing failed: {ex.Message}");
            }
        }

        private object[] BuildStructureMessages(List<byte[]> images)
        {
            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = STRUCTURE_INSTRUCTION }
                    }
                }
            };

            foreach (var img in images)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/png;base64,{Convert.ToBase64String(img)}" }
                        }
                    }
                });
            }

            return messages.ToArray();
        }

        #endregion

        #region Phase 2: Fields per signer

        private async Task<List<string>> ExtractFieldsForSignerAsync(List<byte[]> images, string signerTitle)
        {
            var batches = Batch(images, _config.TemplateExtractorFromPDFConfig.MaxPagesPerBatch);
            var fields = new HashSet<string>();

            foreach (var batch in batches)
            {
                var dto = await CallGptForFieldsBatch(batch, signerTitle);
                if (dto?.Fields == null)
                    continue;

                foreach (var f in dto.Fields)
                {
                    if (!string.IsNullOrWhiteSpace(f))
                        fields.Add(f.Trim());
                }
            }

            return fields.ToList();
        }

        private async Task<SignerFieldsDto> CallGptForFieldsBatch(List<byte[]> images, string signerTitle)
        {
            try
            {
                var instruction = string.Format(FIELDS_INSTRUCTION_TEMPLATE, signerTitle);

                var body = new
                {
                    model = _config.TemplateExtractorFromPDFConfig.Model,
                    messages = BuildFieldsMessages(images, instruction),
                    response_format = new
                    {
                        type = "json_object"
                    }
                };

                string rawResponse = await _config.TemplateExtractorFromPDFConfig.Endpoint
                    .WithHeader("Authorization", $"Bearer {_config.GPTAPIKey}")
                    .WithHeader("Content-Type", "application/json")
                    .PostJsonAsync(body)
                    .ReceiveString();

                using var doc = JsonDocument.Parse(rawResponse);

                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(content))
                    throw new Exception($"GPT returned empty content for signer '{signerTitle}' fields.");

                var parsed = JsonSerializer.Deserialize<SignerFieldsDto>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed == null)
                    throw new Exception($"Parsed GPT fields content is null for signer '{signerTitle}'.");

                parsed.Fields ??= new List<string>();
                return parsed;
            }
            catch (FlurlHttpException httpEx)
            {
                string errorBody = await httpEx.GetResponseStringAsync();
                throw new Exception($"GPT API (fields) returned HTTP {(int?)httpEx.StatusCode} for signer '{signerTitle}': {errorBody}");
            }
            catch (Exception ex)
            {
                throw new Exception($"GPT fields processing failed for signer '{signerTitle}': {ex.Message}");
            }
        }

        private object[] BuildFieldsMessages(List<byte[]> images, string instruction)
        {
            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = instruction }
                    }
                }
            };

            foreach (var img in images)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/png;base64,{Convert.ToBase64String(img)}" }
                        }
                    }
                });
            }

            return messages.ToArray();
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
