namespace ApryseDataExtractor.Models
{
    internal class DocumentStructure
    {
        internal List<Page> Pages { get; set; }
    }

    internal class Page
    {
        internal PageProperties Properties { get; set; }
        internal List<Element> Elements { get; set; }
    }

    internal class Element
    {
        internal string Type { get; set; }
        internal float[] Rect { get; set; }

        internal TextStyle TextStyle { get; set; }
        internal Style Style { get; set; }

        internal List<ContentNode> Contents { get; set; }

        internal List<Row> Trs { get; set; }       // For tables
        internal List<float> ColumnWidths { get; set; }

        internal Table Table { get; set; }
        internal List<Element> NestedElements { get; set; }
    }

    internal class ContentNode
    {
        internal string Type { get; set; }
        internal float[] Rect { get; set; }
        internal string Text { get; set; }
        internal Style Style { get; set; }
        internal List<ContentNode> Contents { get; set; }

        // For nested structures
        internal Table Table { get; set; }
    }

    internal class Style
    {
        internal bool? Bold { get; set; }
        internal int? Weight { get; set; }
        internal bool? Italic { get; set; }
        internal bool? Underline { get; set; }
        internal float? PointSize { get; set; }
        internal string FontFace { get; set; }
    }

    internal class TextStyle
    {
        internal bool? Italic { get; set; }
        internal bool? Underline { get; set; }
        internal float? PointSize { get; set; }
        internal string FontFace { get; set; }
        internal string ParentStyle { get; set; }
    }

    internal class Table
    {
        internal float[] Rect { get; set; }
        internal List<Row> Trs { get; set; }
        internal List<float> ColumnWidths { get; set; }
    }

    internal class Row
    {
        internal float[] Rect { get; set; }
        internal List<Cell> Tds { get; set; }
    }

    internal class Cell
    {
        internal float[] Rect { get; set; }
        internal int RowSpan { get; set; }
        internal int ColSpan { get; set; }
        internal int RowStart { get; set; }
        internal int ColStart { get; set; }
        internal List<ContentNode> Contents { get; set; }
    }

}
