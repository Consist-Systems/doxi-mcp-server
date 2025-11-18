using Consist.GPTDataExtruction.Extensions;
using Consist.GPTDataExtruction.Model;
using Flurl.Http;
//using Flurl.Http.Configuration;
//using Flurl.Http.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using System.Text.Json.Nodes;

namespace Consist.GPTDataExtruction
{
public class GPTDataExtractionClient : IGPTDataExtractionClient
{
        private readonly GPTDataExtructionConfiguration _config;
        //private NewtonsoftJsonSerializer _serializer;

        public GPTDataExtractionClient(GPTDataExtructionConfiguration config)
        {
            //var settings = new JsonSerializerSettings
            //{
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            //};
            //// create a Newtonsoft-based serializer for Flurl to use
            //_serializer = new Flurl.Http.Newtonsoft.NewtonsoftJsonSerializer(settings);

            _config = config;
        }

        public Task<IEnumerable<FieldWithSigner>> ExtractFieldLableToSignerMapping(IEnumerable<SignerInfo> signers, IEnumerable<FieldWithPage> fieldsWithPage, byte[] document)
        {
            throw new NotImplementedException();
        }

        public async Task<CreateTemplateInformation> ExtractTemplateInformation(string templateInstructions)
        {
            var schemaNode = GenerateSchemaNode();
            var payload = new
            {
                model = _config.ExtractTemplateInformationModel,
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

            var response = await "https://api.openai.com/v1/responses"
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


        public class OpenAIResponse
        {
            public List<ResponseOutput> output { get; set; }
        }

        public class ResponseOutput
        {
            public string id { get; set; }
            public string type { get; set; }
            public string status { get; set; }
            public List<ResponseContent> content { get; set; }
        }

        public class ResponseContent
        {
            public string type { get; set; }

            // This is where your JSON string is
            public string text { get; set; }
        }

    }
}