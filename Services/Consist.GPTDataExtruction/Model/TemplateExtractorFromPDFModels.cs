namespace Consist.GPTDataExtruction.Model
{
    public class TemplateInfoFromPDF
    {
        public string TemplateName { get; set; }
        public int? SendMethodType { get; set; }
        public string Languages { get; set; }
        public List<SignerResponse> Signers { get; set; }
    }

    public class TemplateInfoFromPDFwithFields : TemplateInfoFromPDF
    {
        public new List<SignerResponseWithFields> Signers { get; set; }
    }

    public class SignerResponse
    {
        public string Title { get; set; }
        public int? SignerType { get; set; }
    }

    public class SignerResponseWithFields : SignerResponse
    {
        public List<string> Fields { get; set; }
    }
}
