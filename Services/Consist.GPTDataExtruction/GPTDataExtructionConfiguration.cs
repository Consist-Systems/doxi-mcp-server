namespace Consist.GPTDataExtruction
{
    public class GPTDataExtructionConfiguration
    {
        public string GPTAPIKey { get; set; }

        public TemplateExtractorFromPDFConfig TemplateExtractorFromPDFConfig { get; set; }
        public TemplateExtractorFromTextConfig TemplateExtractorFromTextConfig { get; set; }

        public string DefaultModel { get; set; }

        public string DefaultEndpoint { get; set; }

        public JsonSchemaMode DefaultJsonSchemaMode { get; set; }
    }

    public class TemplateExtractorFromPDFConfig
    {
        public string Model { get; set; }
        public string Endpoint { get; set; }
        public int MaxPagesPerBatch { get; set; }
    }

    public class TemplateExtractorFromTextConfig
    {
        public string Model { get; set; }

        public string Endpoint { get; set; }
    }

    public enum JsonSchemaMode
    {
        /// <summary>
        /// Uses structured outputs with json_schema in response_format (recommended for GPT-4o and newer)
        /// </summary>
        StructuredOutput,

        /// <summary>
        /// Uses json_object mode with schema in prompt (fallback for older models)
        /// </summary>
        JsonObject
    }
}
