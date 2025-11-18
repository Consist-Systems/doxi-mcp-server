
namespace Consist.GPTDataExtruction.Model
{
    public class CreateTemplateInformation
    {
        public string? Name { get; set; }

        public string? SenderEmail { get; set; }

        public IEnumerable<SignerInfo> Signers { get; set; }

        public int? SendMethodType { get; set; }
    }

    public class SignerInfo
    {
        public string? Title { get; set; }
        public int? Index { get; set; }
        public int? SignerType { get; set; }
        public FixedSigner? FixedSigner { get; set; }
    }

    public class FixedSigner
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

}
