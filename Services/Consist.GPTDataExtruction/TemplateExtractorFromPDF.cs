using System.Text.Json;
using Consist.GPTDataExtruction.Model;
using Consist.PDFConverter;
using Flurl.Http;

namespace Consist.GPTDataExtruction
{
    public class TemplateExtractorFromPDF
    {
        private readonly GPTDataExtructionConfiguration _config;
        private readonly IDocumentConverter _documentConverter;

        public TemplateExtractorFromPDF(
            GPTDataExtructionConfiguration config,
            IDocumentConverter documentConverter)
        {
            _config = config;
            _documentConverter = documentConverter;
        }

        public async Task<TemplateInfoFromPDF> ExtractAsync(byte[] pdfBytes)
        {
            var images = (await _documentConverter.PDFToImages(pdfBytes)).ToList();
            if (!images.Any())
                throw new Exception("PDF contains no pages or failed to convert to images.");

            var batches = Batch(images, _config.TemplateExtractorFromPDFConfig.MaxPagesPerBatch);

            TemplateInfoFromPDF? final = null;

            foreach (var batch in batches)
            {
                var batchResult = await CallGptForBatch(batch);

                if (final == null)
                {
                    final = batchResult;
                }
                else
                {
                    MergeResponses(final, batchResult);
                }
            }

            if (final == null)
                throw new Exception("No GPT result returned.");

            return final;
        }

        private async Task<TemplateInfoFromPDF> CallGptForBatch(List<byte[]> images)
        {
            try
            {
                var body = new
                {
                    model = _config.TemplateExtractorFromPDFConfig.Model,
                    messages = BuildMessages(images),
                    response_format = new
                    {
                        type = "json_schema",
                        json_schema = BuildJsonSchema()
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
                    throw new Exception("GPT returned empty content.");

                var parsed = JsonSerializer.Deserialize<TemplateInfoFromPDF>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed == null)
                    throw new Exception("GPT JSON parsed into null TemplateResponse.");

                parsed.TemplateName ??= string.Empty;
                parsed.Signers ??= new List<SignerResponse>();

                foreach (var signer in parsed.Signers)
                {
                    signer.Title ??= string.Empty;
                    signer.Fields ??= new List<string>();
                }

                return parsed;
            }
            catch (Exception ex)
            {
                throw new Exception($"GPT processing failed: {ex.Message}");
            }
        }

        private object[] BuildMessages(List<byte[]> images)
        {
            var messages = new List<object>();

            var instruction = @"
You are an expert in document workflow analysis.

You will extract structured metadata from the document images.

### NUMERIC ENUM RULES – MUST FOLLOW EXACTLY:

#### 1. SendMethodType (int)
Return ONLY one of the following numbers:
- 0 → QueuedFlow (sequential signing, one after another)
- 1 → ParallelFlow (all signers can sign independently)

#### 2. SignerType (int)
Return ONLY:
- 0 → Changeable  (signer changes per template instance)
- 1 → Static      (fixed organizational role, always same person)
- 2 → Anonymous   (anyone with a public link can sign)

### You must extract:
1. TemplateName (string)
2. SendMethodType (int)
3. Signers[]:
   - Title (string)
   - SignerType (int)
   - Fields[] (array of string labels)

### Additional rules:
- Fields must contain ONLY field label text. No values.
- Analyze all images as a single multi-page document.
- Output MUST follow the JSON schema provided in `response_format`.
- Output MUST be valid JSON ONLY.";

            messages.Add(new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = instruction }
                }
            });

            foreach (var img in images)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_image",
                            image = Convert.ToBase64String(img)
                        }
                    }
                });
            }

            return messages.ToArray();
        }

        private object BuildJsonSchema()
        {
            return new
            {
                name = "template_pdf_schema",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        TemplateName = new { type = "string" },
                        SendMethodType = new { type = "integer" },
                        Signers = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    Title = new { type = "string" },
                                    SignerType = new { type = "integer" },
                                    Fields = new
                                    {
                                        type = "array",
                                        items = new { type = "string" }
                                    }
                                },
                                required = new[] { "Title", "SignerType", "Fields" }
                            }
                        }
                    },
                    required = new[] { "TemplateName", "SendMethodType", "Signers" }
                }
            };
        }

        private List<List<byte[]>> Batch(List<byte[]> images, int batchSize)
        {
            var result = new List<List<byte[]>>();

            for (int i = 0; i < images.Count; i += batchSize)
                result.Add(images.Skip(i).Take(batchSize).ToList());

            return result;
        }

        private void MergeResponses(TemplateInfoFromPDF final, TemplateInfoFromPDF add)
        {
            foreach (var signer in add.Signers)
            {
                var existing = final.Signers.FirstOrDefault(s =>
                    s.Title == signer.Title &&
                    s.SignerType == signer.SignerType);

                if (existing == null)
                {
                    final.Signers.Add(signer);
                }
                else
                {
                    foreach (var field in signer.Fields)
                    {
                        if (!existing.Fields.Contains(field))
                            existing.Fields.Add(field);
                    }
                }
            }
        }
    }
}
