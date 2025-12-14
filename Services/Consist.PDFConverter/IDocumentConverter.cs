namespace Consist.PDFTools
{
    public interface IDocumentConverter
    {
        Task<IEnumerable<byte[]>> PDFToImages(byte[] pdfFile);
    }
}
