namespace ApryseDataExtractor.Models
{
    internal class DocumentFieldsPosition
    {
        internal DocumentProperties Properties { get; set; }
        internal RunInfo RunInfo { get; set; }
        internal List<PdfPage> Pages { get; set; }
    }

    internal class DocumentProperties
    {
        internal string Producer { get; set; }
        internal string SchemaVersion { get; set; }
        internal string CoordinateSystem { get; set; }
    }

    internal class RunInfo
    {
        internal List<string> Warnings { get; set; }
        internal string ProducerPlatform { get; set; }
        internal string ProducerVersion { get; set; }
    }

    internal class PdfPage
    {
        internal PageProperties Properties { get; set; }
        internal List<FormElement> FormElements { get; set; }
    }

    internal class PageProperties
    {
        internal int PageNumber { get; set; }
    }

    internal class FormElement
    {
        internal string Type { get; set; }
        internal float Confidence { get; set; }

        /// <summary>
        /// rect = [x1, y1, x2, y2]
        /// </summary>
        internal List<float> Rect { get; set; }
    }

}
