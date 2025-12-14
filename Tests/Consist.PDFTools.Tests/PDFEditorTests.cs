using Consist.PDFTools;
using Consist.PDFTools.Model;
using Syncfusion.Licensing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace Consist.PDFTools.Tests
{
    public class PDFEditorTests
    {
        [SetUp]
        public void Setup()
        {
            SyncfusionLicenseProvider.RegisterLicense("NxYtFisQPR08Cit/Vkd+XU9FcVRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS3tSd0dlWH9ac3ZQQGZUV091Vg==");
        }

        [Test]
        public async Task AddTexts_ShouldAddTextToPdf()
        {
            // Arrange
            var pdfEditor = new PDFEditor();
            var pdfBytes = CreateSimplePdf();
            var textElements = new List<TextElement>
            {
                new TextElement
                {
                    Text = "Test Text",
                    Font = "Helvetica",
                    FontSize = 12,
                    X = 100,
                    Y = 100,
                    Width = 200,
                    Height = 20,
                    PageNumber = 1
                },
                new TextElement
                {
                    Text = "Test Text",
                    Font = "times",
                    FontSize = 12,
                    X = 200,
                    Y = 200,
                    Width = 200,
                    Height = 20,
                    PageNumber = 1
                }
            };

            // Act
            var result = await pdfEditor.AddTexts(pdfBytes, textElements);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));

            // Save the result PDF to the test running folder
            var outputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "AddTexts_Result.pdf");
            await File.WriteAllBytesAsync(outputPath, result);
            TestContext.WriteLine($"PDF saved to: {outputPath}");
        }

        private byte[] CreateSimplePdf()
        {
            using var document = new PdfDocument();
            document.Pages.Add();
            
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }
    }
}
