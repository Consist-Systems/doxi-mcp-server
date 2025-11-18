namespace Consist.GPTDataExtruction.Model
{
    public class FieldWithSigner : FieldWithPage
    {
        public FieldWithSigner(int page, string elementId,int signerIndex) : base(page, elementId)
        {
            SignerIndex = signerIndex;
        }

        public int SignerIndex { get; private set; }
    }
}
