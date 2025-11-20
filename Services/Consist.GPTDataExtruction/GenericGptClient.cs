using Consist.GPTDataExtruction.Extensions;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Generation;
using System.Text.Json;

namespace Consist.GPTDataExtruction
{
    public class GenericGptClient
    {
        private readonly ILogger<GenericGptClient> _logger;
        private readonly GPTDataExtructionConfiguration _config;

        private string _apiKey;
        private string _currentEndpoint;
        private string _currentModel;
        private JsonSchemaMode _currentSchemaMode;

        public GenericGptClient(
            ILogger<GenericGptClient> logger,
            GPTDataExtructionConfiguration config)
        {
            _logger = logger;
            _config = config;

            // Initialize with default values from config
            _apiKey = config.GPTAPIKey;
            _currentEndpoint = config.DefaultEndpoint;
            _currentModel = config.DefaultModel;
            _currentSchemaMode = config.DefaultJsonSchemaMode;
        }

        /// <summary>
        /// Updates the model, endpoint, and schema mode for subsequent calls
        /// </summary>
        public void SetModelData(string? model = null, string? endpoint = null, JsonSchemaMode? schemaMode = null)
        {
            if (!string.IsNullOrWhiteSpace(model))
                _currentModel = model;

            if (!string.IsNullOrWhiteSpace(endpoint))
                _currentEndpoint = endpoint;

            if (schemaMode.HasValue)
                _currentSchemaMode = schemaMode.Value;

            _logger.LogDebug("Model data updated - Model: {Model}, Endpoint: {Endpoint}, SchemaMode: {SchemaMode}",
                _currentModel, _currentEndpoint, _currentSchemaMode);
        }

        /// <summary>
        /// Calls GPT with text input and returns structured JSON response
        /// </summary>
        public async Task<TResponse> RunModelByText<TResponse>(
            string inputData,
            string instructions) where TResponse : class
        {
            var schema = GenerateJsonSchema<TResponse>();

            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = BuildInstructionWithSchema(instructions, schema) },
                        new { type = "text", text = inputData }
                    }
                }
            };

            return await CallGptAsync<TResponse>(messages.ToArray(), schema);
        }

        /// <summary>
        /// Calls GPT with file(s) input (images/PDFs) and returns structured JSON response
        /// </summary>
        public async Task<TResponse> RunModelByFiles<TResponse>(
            IEnumerable<byte[]> inputData,
            string instructions,
            string fileType = "image/png") where TResponse : class
        {
            var schema = GenerateJsonSchema<TResponse>();

            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = BuildInstructionWithSchema(instructions, schema) }
                    }
                }
            };

            foreach (var file in inputData)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:{fileType};base64,{Convert.ToBase64String(file)}" }
                        }
                    }
                });
            }

            return await CallGptAsync<TResponse>(messages.ToArray(), schema);
        }

        /// <summary>
        /// Core method that makes the actual GPT API call
        /// </summary>
        private async Task<TResponse> CallGptAsync<TResponse>(object[] messages, string schema) where TResponse : class
        {
            try
            {
                object body;

                if (_currentSchemaMode == JsonSchemaMode.StructuredOutput)
                {
                    // Use structured outputs (recommended for GPT-4o and newer)
                    var schemaJson = JsonSerializer.Deserialize<JsonElement>(schema);

                    body = new
                    {
                        model = _currentModel,
                        messages = messages,
                        response_format = new
                        {
                            type = "json_schema",
                            json_schema = new
                            {
                                name = typeof(TResponse).Name.ToLower() + "_response",
                                schema = schemaJson,
                                strict = true
                            }
                        }
                    };
                }
                else
                {
                    // Fallback to json_object mode (for older models)
                    body = new
                    {
                        model = _currentModel,
                        messages = messages,
                        response_format = new
                        {
                            type = "json_object"
                        }
                    };
                }

                _logger.LogDebug("Calling GPT API with model: {Model}, schema mode: {SchemaMode}",
                    _currentModel, _currentSchemaMode);

                string rawResponse = await _currentEndpoint
                    .WithHeader("Authorization", $"Bearer {_apiKey}")
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

                _logger.LogDebug("GPT Response: {Content}", content);

                var parsed = JsonSerializer.Deserialize<TResponse>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed == null)
                    throw new Exception("Failed to deserialize GPT response to the expected type.");

                return parsed;
            }
            catch (FlurlHttpException httpEx)
            {
                string errorBody = await httpEx.GetResponseStringAsync();
                _logger.LogError(httpEx, "GPT API returned HTTP {StatusCode}: {ErrorBody}",
                    (int?)httpEx.StatusCode, errorBody);
                throw new Exception($"GPT API returned HTTP {(int?)httpEx.StatusCode}: {errorBody}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GPT processing failed");
                throw new Exception($"GPT processing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates JSON schema for the response type
        /// </summary>
        private string GenerateJsonSchema<TResponse>() where TResponse : class
        {
            var schemaNode = JsonSchema.FromType<TResponse>().ToOpenAISchemaNode<TResponse>();
            return schemaNode.ToJsonString();
        }

        /// <summary>
        /// Combines user instructions with JSON schema requirements
        /// </summary>
        private string BuildInstructionWithSchema(string instructions, string jsonSchema)
        {
            if (_currentSchemaMode == JsonSchemaMode.StructuredOutput)
            {
                // Schema is enforced via response_format, no need in prompt
                return instructions;
            }
            else
            {
                // Include schema in prompt for json_object mode
                return $@"{instructions}

You MUST return a valid JSON response that matches this exact schema:

{jsonSchema}

Return ONLY valid JSON that conforms to this schema, with no additional text or explanation.";
            }
        }
    }
}