namespace Consist.GPTDataExtruction.Model
{
    public class TextElement 
    {
        public string Text { get; set; }

        public string Font { get; set; }

        public double FontSize { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public int PageNumber { get; set; }

    }

    public class RootTextElement
    {
        public TextElement[] TextElementArray { get; set; }
    }
}
