using Consist.GPTDataExtruction.Extensions;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace Consist.GPTDataExtruction
{
    public class GenericGptClient
    {
        private readonly ILogger<GenericGptClient> _logger;

        private string _apiKey;
        private string _currentEndpoint;
        private string _currentModel;

        public GenericGptClient(
            ILogger<GenericGptClient> logger,
            GPTDataExtructionConfiguration config)
        {
            _logger = logger;

            // Initialize with default values from config
            _apiKey = config.GPTAPIKey;
            _currentEndpoint = config.DefaultEndpoint;
            _currentModel = config.DefaultModel;
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


            _logger.LogDebug($"Model data updated - Model: {_currentModel}, Endpoint: {_currentEndpoint}",
                _currentModel, _currentEndpoint);
        }

        /// <summary>
        /// Calls GPT with text input and returns structured JSON response
        /// </summary>
        public async Task<TResponse> RunModelByText<TResponse>(
            string instructions) where TResponse : class
        {
            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = instructions }
                    }
                }
            };

            return await CallGptAsync<TResponse>(messages.ToArray());
        }

        /// <summary>
        /// Calls GPT with file(s) input (images/PDFs) and returns structured JSON response
        /// </summary>  
        public async Task<TResponse> RunModelByFiles<TResponse>(
            IEnumerable<byte[]> inputData,
            string instructions,
            string fileType = "image/png") where TResponse : class
        {

            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = instructions }
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
                            type = "input_image",
                            image_url = $"data:{fileType};base64,{Convert.ToBase64String(file)}"
                        }
                    }
                });
            }

            return await CallGptAsync<TResponse>(messages.ToArray());
        }

        /// <summary>
        /// Core method that makes the actual GPT API call
        /// </summary>
        private async Task<TResponse> CallGptAsync<TResponse>(object[] messages) where TResponse : class
        {
            try
            {
                var schema = GenerateJsonSchema<TResponse>();

                var payload = new
                    {
                        model = _currentModel,
                        input = messages,
                        text = new
                        {
                            format = new
                            {
                                type = "json_schema",
                                name = typeof(TResponse).Name,
                                schema = schema,
                                strict = true
                            }
                        }
                    };
               
                var payloadStr = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug($"GPT Request Payload: {payloadStr}");

                string rawResponse = await _currentEndpoint
                    .WithHeader("Authorization", $"Bearer {_apiKey}")   
                    .WithHeader("Content-Type", "application/json")
                    .PostStringAsync(payloadStr)
                    .ReceiveString();

                using var doc = System.Text.Json.JsonDocument.Parse(rawResponse);

                // Check for error response
                if (doc.RootElement.TryGetProperty("error", out var errorElement)
                    && errorElement.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    var errorMessage = errorElement.TryGetProperty("message", out var messageProp) 
                        ? messageProp.GetString() 
                        : "Unknown error";
                    throw new Exception($"GPT API returned an error: {errorMessage}");
                }

                var parsed = ParseGptResponse<TResponse>(rawResponse);
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

        public static TResponse ParseGptResponse<TResponse>(string rawResponse)
    where TResponse : class
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawResponse);

            // Navigate to: output[0].content[0].text
            var output = doc.RootElement.GetProperty("output");

            if (output.GetArrayLength() == 0)
                throw new Exception("GPT response has no output.");

            var message = output[0];

            var contentArray = message.GetProperty("content");
            if (contentArray.GetArrayLength() == 0)
                throw new Exception("No content in GPT response.");

            var contentItem = contentArray[0];

            var jsonText = contentItem.GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(jsonText))
                throw new Exception("GPT response text is empty.");

            // Parse the JSON string inside "text"
            return System.Text.Json.JsonSerializer.Deserialize<TResponse>(
                jsonText,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;
        }


        private System.Text.Json.JsonElement GenerateJsonSchema<TResponse>() where TResponse : class
        {
            // Use MIT-licensed NJsonSchema to avoid Newtonsoft.Json.Schema licensing limits
            System.Text.Json.Nodes.JsonNode schemaNode = new NJsonSchema.JsonSchema().ToOpenAISchemaNode<TResponse>();

            using var doc = System.Text.Json.JsonDocument.Parse(schemaNode.ToJsonString());
            return doc.RootElement.Clone();
        }

        public async Task<TResponse> RunModelByFiles<TResponse>(IEnumerable<byte[]> requestFiles,IEnumerable<string> inputDatas, string instructions)
            where TResponse : class
        {
            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = instructions }
                    }
                }
            };

            foreach (var file in requestFiles)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_image",
                            image_url = $"data:image/png;base64,{Convert.ToBase64String(file)}"
                        }
                    }
                });
            }

            foreach (var inputData in inputDatas)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = inputData
                        }
                    }
                });
            }

            return await CallGptAsync<TResponse>(messages.ToArray());
        }
    }
}