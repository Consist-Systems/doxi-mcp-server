namespace Consist.GPTDataExtruction
{
    public class GPTDataExtructionConfiguration
    {
        public string GPTAPIKey { get; set; }

        public TemplateExtractorFromPDFConfig TemplateExtractorFromPDFConfig { get; set; }
        public TemplateExtractorFromTextConfig TemplateExtractorFromTextConfig { get; set; }
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
}
