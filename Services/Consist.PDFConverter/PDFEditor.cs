using Consist.PDFTools.Model;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;
using System.Drawing;

namespace Consist.PDFTools
{
    public class PDFEditor : IPDFEditor
    {
        public async Task<byte[]> AddTexts(byte[] pdfFile, IEnumerable<TextElement> elements)
        {
            return await Task.Run(() =>
            {
                using var inputStream = new MemoryStream(pdfFile);
                using var loadedDocument = new PdfLoadedDocument(inputStream);
                ProcessTextElements(loadedDocument, elements);
                return SaveDocument(loadedDocument);
            });
        }

        private void ProcessTextElements(PdfLoadedDocument document, IEnumerable<TextElement> elements)
        {
            foreach (var element in elements)
            {
                var page = GetPage(document, element.PageNumber);
                if (page != null)
                {
                    AddTextToPage(page, element);
                }
            }
        }

        private PdfLoadedPage? GetPage(PdfLoadedDocument document, int pageNumber)
        {
            int pageIndex = pageNumber - 1;
            if (pageIndex < 0 || pageIndex >= document.Pages.Count)
            {
                return null;
            }
            return document.Pages[pageIndex] as PdfLoadedPage;
        }

        private void AddTextToPage(PdfLoadedPage page, TextElement element)
        {
            var graphics = page.Graphics;
            var font = CreateFont(element);
            var brush = new PdfSolidBrush(new PdfColor(0, 0, 0));
            var yPosition = CalculateYPosition(page, element);
            var rectangle = CreateRectangleF((float)element.X, yPosition, (float)element.Width, (float)element.Height);
            graphics.DrawString(element.Text ?? string.Empty, font, brush, rectangle);
        }

        private PdfFont CreateFont(TextElement element)
        {
            if (string.IsNullOrWhiteSpace(element.Font))
            {
                return new PdfStandardFont(PdfFontFamily.Helvetica, element.FontSize);
            }

            try
            {
                return new PdfStandardFont(GetFontFamily(element.Font), element.FontSize);
            }
            catch
            {
                return new PdfStandardFont(PdfFontFamily.Helvetica, element.FontSize);
            }
        }

        private float CalculateYPosition(PdfLoadedPage page, TextElement element)
        {
            var pageSize = page.Size;
            return (float)(pageSize.Height - element.Y - element.Height);
        }

        private byte[] SaveDocument(PdfLoadedDocument document)
        {
            using var outputStream = new MemoryStream();
            document.Save(outputStream);
            document.Close(true);
            return outputStream.ToArray();
        }

        private PdfFontFamily GetFontFamily(string fontName)
        {
            // Map common font names to Syncfusion font families
            var fontLower = fontName.ToLowerInvariant();
            return fontLower switch
            {
                "helvetica" or "arial" => PdfFontFamily.Helvetica,
                "times" or "times roman" or "times new roman" => PdfFontFamily.TimesRoman,
                "courier" or "courier new" => PdfFontFamily.Courier,
                "symbol" => PdfFontFamily.Symbol,
                "zapfdingbats" => PdfFontFamily.ZapfDingbats,
                _ => PdfFontFamily.Helvetica // Default fallback
            };
        }

        // Helper method to create RectangleF and avoid type ambiguity between Syncfusion assemblies
        private dynamic CreateRectangleF(float x, float y, float width, float height)
        {
            // Search in all loaded assemblies for the RectangleF type
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name?.Contains("Syncfusion") == true)
                {
                    var rectangleType = assembly.GetType("Syncfusion.Drawing.RectangleF");
                    if (rectangleType != null)
                    {
                        var instance = Activator.CreateInstance(rectangleType, x, y, width, height);
                        if (instance != null)
                        {
                            return instance;
                        }
                    }
                }
            }
            
            // Fallback - this shouldn't happen but provides safety
            throw new InvalidOperationException("Unable to create Syncfusion.Drawing.RectangleF");
        }
    }
}

