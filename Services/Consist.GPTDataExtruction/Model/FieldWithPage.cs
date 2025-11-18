namespace Consist.GPTDataExtruction.Model
{
    public class FieldWithPage
    {
        public FieldWithPage(int page,string elementId)
        {
            ElementId = elementId;
            Page = page;
        }

        public int Page { get; private set; }

        public string ElementId { get; private set; }

    }
}
