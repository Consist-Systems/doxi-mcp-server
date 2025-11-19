using Consist.GPTDataExtruction.Extensions;
using Consist.GPTDataExtruction.Model;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;


namespace Consist.GPTDataExtruction
{
    public class TemplateExtractorFromText
    {
        private readonly GPTDataExtructionConfiguration _config;

        public TemplateExtractorFromText(GPTDataExtructionConfiguration config)
        {
            _config = config;
        }

        public async Task<CreateTemplateInformation> ExtractAsync(string templateInstructions)
        {
            var schemaNode = GenerateSchemaNode();
            var payload = new
            {
                model = _config.TemplateExtractorFromTextConfig.Model,
                input = templateInstructions,
                text = new
                {
                    format = new
                    {
                        name = "CreateTemplateInformation",
                        type = "json_schema",
                        schema = schemaNode
                    }
                }
            };

            var response = await _config.TemplateExtractorFromTextConfig.Endpoint
                    .WithOAuthBearerToken(_config.GPTAPIKey)
                    //.WithSettings(settings => settings.JsonSerializer = _serializer)
                    .AllowAnyHttpStatus()
                    .PostJsonAsync(payload);

            var body = await response.GetStringAsync();

            if (response.StatusCode != 200)
                throw new Exception($"GPT API Error: {body}");

            try
            {
                var openAIChatResponse = JsonConvert.DeserializeObject<OpenAIResponse>(body);
                var result = openAIChatResponse.output[0].content[0].text;
                return JsonConvert.DeserializeObject<CreateTemplateInformation>(result);
            }
            catch
            {
                throw new Exception($"Error:{body}");
            }
        }

        public JObject GenerateSchemaNode()
        {
            var schemaNode = JsonSchema
                .FromType<CreateTemplateInformation>()       // create schema
                .ToOpenAISchemaNode<CreateTemplateInformation>();  // fix schema + convert to JsonNode

            var schemaJsonString = schemaNode.ToJsonString();

            return JObject.Parse(schemaJsonString);
        }
    }
}
