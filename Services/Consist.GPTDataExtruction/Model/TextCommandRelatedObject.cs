namespace Consist.GPTDataExtruction.Model
{
    public class TextCommandRelatedObjectsRoot
    {
        public TextCommandRelatedObjects[] TextCommandsRelatedObjects { get; set; }
    }

    public class TextCommandRelatedObjects
    {
        public string Command { get; set; }
        public TextCommandRelatedObject<string>[] TextCommandRelatedObject { get; set; }
    }

    public class TextCommandRelatedObject<T>
    {
        public int PageNumber { get; set; }

        public T ReferenceElement { get; set; }
    }

    public class TextCommandRelatedObjectsWithText : TextCommandRelatedObjects
    {
        public string Text { get; set; }

        public new TextCommandRelatedObject<dynamic>[] TextCommandRelatedObject { get; set; }
    }
}
