namespace ApryseDataExtractor.Models
{
    public class DocumentFieldsPosition
    {
        public DocumentProperties Properties { get; set; }
        public RunInfo RunInfo { get; set; }
        public List<PdfPage> Pages { get; set; }
    }

    public class DocumentProperties
    {
        public string Producer { get; set; }
        public string SchemaVersion { get; set; }
        public string CoordinateSystem { get; set; }
    }

    public class RunInfo
    {
        public List<string> Warnings { get; set; }
        public string ProducerPlatform { get; set; }
        public string ProducerVersion { get; set; }
    }

    public class PdfPage
    {
        public PageProperties Properties { get; set; }
        public List<FormElement> FormElements { get; set; }
    }

    public class PageProperties
    {
        public int PageNumber { get; set; }
    }

    public class FormElement
    {
        public string Type { get; set; }
        public float Confidence { get; set; }

        /// <summary>
        /// rect = [x1, y1, x2, y2]
        /// </summary>
        public List<float> Rect { get; set; }
    }

}
