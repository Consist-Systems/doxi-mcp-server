namespace Consist.GPTDataExtruction.Model
{
    public class TextElementInstructionsRoot
    {
        public TextElementInstructions[] TextElementInstructions { get; set; }
    }

    public class TextElementInstructions
    {
        public string Text { get; set; }

        public string Command { get; set; }

        public bool IsInField { get; set; }
    }
}
