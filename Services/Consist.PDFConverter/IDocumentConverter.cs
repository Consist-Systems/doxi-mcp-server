namespace Consist.PDFConverter
{
    public interface IDocumentConverter
    {
        Task<IEnumerable<byte[]>> PDFToImages(byte[] pdfFile);
    }
}
