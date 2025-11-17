namespace Consist.GPTDataExtruction.Model
{
    public class FieldWithSigner : FieldWithPage
    {
        public FieldWithSigner(int page, string lable,string signerTitle) : base(page,lable )
        {
            SignerTitle = signerTitle;
        }

        public string SignerTitle { get; private set; }
    }
}
