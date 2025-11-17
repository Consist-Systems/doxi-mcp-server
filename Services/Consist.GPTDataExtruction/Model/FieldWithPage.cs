namespace Consist.GPTDataExtruction.Model
{
    public class FieldWithPage
    {
        public FieldWithPage(int page,string lable)
        {
            Lable = lable;
            Page = page;
        }

        public int Page { get; private set; }

        public string Lable { get; private set; }

    }
}
