using System.ComponentModel.DataAnnotations;

namespace Consist.GPTDataExtruction.Model
{
    public class TemplateInfoFromPDF
    {
        [Required]
        public string TemplateName { get; set; }
        public int? SendMethodType { get; set; }
        [Required]
        public string Languages { get; set; }
        public SignerResponse[] Signers { get; set; }
    }

    public class TemplateInfoFromPDFwithFields : TemplateInfoFromPDF
    {
        public new SignerResponseWithFields[] Signers { get; set; }
    }

    public class SignerResponse
    {
        public string Title { get; set; }
        public int? SignerType { get; set; }
    }

    public class SignerResponseWithFields : SignerResponse
    {
        public string[] Fields { get; set; }
    }
}
