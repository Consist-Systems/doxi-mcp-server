namespace Consist.GPTDataExtruction.Model
{
    public class TemplateInfoFromPDF
    {
        public string TemplateName { get; set; }
        public int? SendMethodType { get; set; }
        public List<SignerResponse> Signers { get; set; }
    }

    public class SignerResponse
    {
        public string Title { get; set; }
        public int? SignerType { get; set; }
        public List<string> Fields { get; set; } 
    }
}
