using Syncfusion.PdfToImageConverter;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Consist.PDFConverter
{
    public class SyncfusionDocumentConverter : IDocumentConverter
    {
        public Task<IEnumerable<byte[]>> PDFToImages(byte[] pdfFile)
        {
            var result = new List<byte[]>();

            using var inputStream = new MemoryStream(pdfFile);

            // Initialize the converter (matching your sample)
            using var converter = new PdfToImageConverter();

            // Load document
            converter.Load(inputStream);

            int pageCount = converter.PageCount;

            for (int i = 0; i < pageCount; i++)
            {
                // Convert single page to stream
                var outputStream = converter.Convert(i, false, false);

                // Rewind stream before reading
                outputStream.Position = 0;

                using var ms = new MemoryStream();
                outputStream.CopyTo(ms);

                result.Add(ms.ToArray());

                outputStream.Dispose();
            }

            return Task.FromResult<IEnumerable<byte[]>>(result);
        }
    }
}
