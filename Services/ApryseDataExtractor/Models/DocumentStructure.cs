namespace ApryseDataExtractor.Models
{
    public class DocumentStructure
    {
        public List<Page> Pages { get; set; }
    }

    public class Page
    {
        public PageProperties Properties { get; set; }
        public List<Element> Elements { get; set; }
    }

    public class Element
    {
        public string Type { get; set; }
        public float[] Rect { get; set; }

        public TextStyle TextStyle { get; set; }
        public Style Style { get; set; }

        public List<ContentNode> Contents { get; set; }

        public List<Row> Trs { get; set; }       // For tables
        public List<float> ColumnWidths { get; set; }

        public Table Table { get; set; }
        public List<Element> NestedElements { get; set; }
    }

    public class ContentNode
    {
        public string Type { get; set; }
        public float[] Rect { get; set; }
        public string Text { get; set; }
        public Style Style { get; set; }
        public List<ContentNode> Contents { get; set; }

        // For nested structures
        public Table Table { get; set; }
    }

    public class Style
    {
        public bool? Bold { get; set; }
        public int? Weight { get; set; }
        public bool? Italic { get; set; }
        public bool? Underline { get; set; }
        public float? PointSize { get; set; }
        public string FontFace { get; set; }
    }

    public class TextStyle
    {
        public bool? Italic { get; set; }
        public bool? Underline { get; set; }
        public float? PointSize { get; set; }
        public string FontFace { get; set; }
        public string ParentStyle { get; set; }
    }

    public class Table
    {
        public float[] Rect { get; set; }
        public List<Row> Trs { get; set; }
        public List<float> ColumnWidths { get; set; }
    }

    public class Row
    {
        public float[] Rect { get; set; }
        public List<Cell> Tds { get; set; }
    }

    public class Cell
    {
        public float[] Rect { get; set; }
        public int RowSpan { get; set; }
        public int ColSpan { get; set; }
        public int RowStart { get; set; }
        public int ColStart { get; set; }
        public List<ContentNode> Contents { get; set; }
    }

}
