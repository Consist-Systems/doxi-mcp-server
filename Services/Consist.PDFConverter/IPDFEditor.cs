using Consist.PDFTools.Model;

namespace Consist.PDFTools
{
    public interface IPDFEditor
    {
        Task<byte[]> AddTexts(byte[] pdfFile,IEnumerable<TextElement> elements);
    }
}
