using ApryseDataExtractor.Models;
using Consist.Doxi.Domain.Models;
using Consist.Doxi.Enums;
using Newtonsoft.Json;
using pdftron;
using pdftron.PDF;

namespace ApryseDataExtractor
{
    public class DocumentFieldExtractor : IDocumentFieldExtractor
    {
        public DocumentFieldExtractor(ApryseDataExtractorConfiguration config)
        {
            PDFNet.Initialize(config.ApryseApiKey);
        }

        public async Task<IEnumerable<ExTemplatFlowElement>> GetDocumentElements(
            byte[] documentBytes,
            string languages)
        {
            var tempFilePath = Path.GetTempFileName().Replace(".tmp", ".pdf");
            File.WriteAllBytes(tempFilePath, documentBytes);

            try
            {
                DataExtractionOptions options = new DataExtractionOptions();
                options.SetLanguage(languages);

                var documentFieldsPositionJson =
                    DataExtractionModule.ExtractData(
                        tempFilePath,
                        DataExtractionModule.DataExtractionEngine.e_form,
                        options);

                var documentFieldsPosition =
                    JsonConvert.DeserializeObject<DocumentFieldsPosition>(documentFieldsPositionJson);

                var documentStructureJson =
                    DataExtractionModule.ExtractData(
                        tempFilePath,
                        DataExtractionModule.DataExtractionEngine.e_doc_structure,
                        options);

                var documentStructure =
                    JsonConvert.DeserializeObject<DocumentStructure>(documentStructureJson);

                return GetMatchFieldToLabel(documentFieldsPosition, documentStructure);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }


        // --------------------------------------------------------------------
        // MATCH FIELD TO LABEL
        // --------------------------------------------------------------------
        private IEnumerable<ExTemplatFlowElement> GetMatchFieldToLabel(
            DocumentFieldsPosition? documentFieldsPosition,
            DocumentStructure? documentStructure)
        {
            if (documentFieldsPosition == null || documentStructure == null)
                return Enumerable.Empty<ExTemplatFlowElement>();

            var textBlocks = ExtractTextBlocks(documentStructure);
            var fieldRectangles = ExtractFieldRectangles(documentFieldsPosition);

            return fieldRectangles.Select(field => MatchFieldToLabel(field, textBlocks));
        }


        // --------------------------------------------------------------------
        // EXTRACT TEXT BLOCKS
        // --------------------------------------------------------------------
        private List<TextBlock> ExtractTextBlocks(DocumentStructure documentStructure)
        {
            var textBlocks = new List<TextBlock>();

            if (documentStructure.Pages == null)
                return textBlocks;

            foreach (var page in documentStructure.Pages)
            {
                var pageNumber = page.Properties?.PageNumber ?? 0;

                if (page.Elements == null)
                    continue;

                foreach (var element in page.Elements)
                {
                    ExtractTextFromElement(element, pageNumber, textBlocks);
                }
            }

            return textBlocks;
        }

        private void ExtractTextFromElement(Models.Element element, int pageNumber, List<TextBlock> textBlocks)
        {
            if (element.Contents != null)
            {
                foreach (var content in element.Contents)
                    ExtractTextFromContentNode(content, pageNumber, textBlocks);
            }

            if (element.NestedElements != null)
            {
                foreach (var nested in element.NestedElements)
                    ExtractTextFromElement(nested, pageNumber, textBlocks);
            }

            if (element.Table?.Trs != null)
            {
                foreach (var row in element.Table.Trs)
                {
                    if (row.Tds == null) continue;

                    foreach (var cell in row.Tds)
                    {
                        if (cell.Contents == null) continue;

                        foreach (var content in cell.Contents)
                            ExtractTextFromContentNode(content, pageNumber, textBlocks);
                    }
                }
            }
        }

        private void ExtractTextFromContentNode(ContentNode node, int pageNumber, List<TextBlock> textBlocks)
        {
            if (!string.IsNullOrWhiteSpace(node.Text)
                && node.Rect != null
                && node.Rect.Length >= 4)
            {
                textBlocks.Add(new TextBlock
                {
                    Text = node.Text.Trim().FixRtl(),   // ← APPLY RTL FIX
                    X1 = node.Rect[0],
                    Y1 = node.Rect[1],
                    X2 = node.Rect[2],
                    Y2 = node.Rect[3],
                    PageNumber = pageNumber
                });
            }

            if (node.Contents != null)
            {
                foreach (var nested in node.Contents)
                    ExtractTextFromContentNode(nested, pageNumber, textBlocks);
            }

            if (node.Table?.Trs != null)
            {
                foreach (var row in node.Table.Trs)
                {
                    if (row.Tds == null) continue;

                    foreach (var cell in row.Tds)
                    {
                        if (cell.Contents == null) continue;

                        foreach (var content in cell.Contents)
                            ExtractTextFromContentNode(content, pageNumber, textBlocks);
                    }
                }
            }
        }


        // --------------------------------------------------------------------
        // FIELD RECTANGLES
        // --------------------------------------------------------------------
        private List<FieldRectangle> ExtractFieldRectangles(DocumentFieldsPosition documentFieldsPosition)
        {
            var result = new List<FieldRectangle>();

            if (documentFieldsPosition.Pages == null)
                return result;

            foreach (var page in documentFieldsPosition.Pages)
            {
                var pageNumber = page.Properties?.PageNumber ?? 0;

                if (page.FormElements == null)
                    continue;

                foreach (var formElement in page.FormElements)
                {
                    if (formElement.Rect == null || formElement.Rect.Count < 4)
                        continue;

                    var rect = formElement.Rect;

                    result.Add(new FieldRectangle
                    {
                        X1 = rect[0],
                        Y1 = rect[1],
                        X2 = rect[2],
                        Y2 = rect[3],
                        PageNumber = pageNumber,
                        FieldType = formElement.Type
                    });
                }
            }

            return result;
        }


        // --------------------------------------------------------------------
        // MATCH FIELD → LABEL
        // --------------------------------------------------------------------
        private ExTemplatFlowElement MatchFieldToLabel(FieldRectangle field, List<TextBlock> textBlocks)
        {
            var samePage = textBlocks.Where(tb => tb.PageNumber == field.PageNumber).ToList();

            if (samePage.Count == 0)
                return CreateElement(field, field.FieldType, null);

            var overlapping = samePage.Where(tb => OverlapsVertically(field, tb)).ToList();

            if (overlapping.Count == 0)
                return CreateElement(field, field.FieldType, null);

            var rightSide = overlapping.Where(tb => tb.X1 >= field.X2 - 3).ToList();
            var candidates = rightSide.Any() ? rightSide : overlapping;

            var valid = candidates
                .Where(tb => !string.IsNullOrWhiteSpace(tb.Text)
                             && !IsPunctuationOnly(tb.Text))
                .ToList();

            if (!valid.Any())
                valid = candidates.Where(tb => !string.IsNullOrWhiteSpace(tb.Text)).ToList();

            if (!valid.Any())
                return CreateElement(field, field.FieldType, null);

            var best = valid.OrderBy(tb => HorizontalDistance(field, tb)).FirstOrDefault();

            return CreateElement(field, field.FieldType, best?.Text);
        }


        // --------------------------------------------------------------------
        // HELPERS
        // --------------------------------------------------------------------
        private bool OverlapsVertically(FieldRectangle field, TextBlock text)
        {
            float overlap = Math.Min(field.Y2, text.Y2) - Math.Max(field.Y1, text.Y1);
            if (overlap <= 0) return false;

            float fieldH = field.Y2 - field.Y1;
            float textH = text.Y2 - text.Y1;
            float minH = Math.Min(fieldH, textH);

            return overlap >= (minH * 0.30f);
        }

        private float HorizontalDistance(FieldRectangle f, TextBlock t)
        {
            if (t.X1 >= f.X2) return t.X1 - f.X2;
            if (t.X2 <= f.X1) return f.X1 - t.X2;

            return Math.Min(Math.Abs(t.X1 - f.X1), Math.Abs(t.X2 - f.X2));
        }

        private bool IsPunctuationOnly(string text)
        {
            return text.Trim().All(c => char.IsPunctuation(c));
        }

        private ExTemplatFlowElement CreateElement(FieldRectangle field, string fieldType, string? label)
        {
            return new ExTemplatFlowElement
            {
                ElementId = Guid.NewGuid().ToString(),
                PageNumber = field.PageNumber,

                Position = new ElementPosition
                {
                    Left = field.X1,
                    Top = field.Y1,
                    Width = field.X2 - field.X1,
                    Height = field.Y2 - field.Y1
                },

                ElementLabel = label,
                ElementType = MapElementType(fieldType)
            };
        }

        private ElementType MapElementType(string type)
        {
            return type switch
            {
                "formCheckBox" => ElementType.Checkbox,
                "formTextField" => ElementType.Text,
                "formDigitalSignature" => ElementType.Sign,
                _ => ElementType.Text
            };
        }


        // --------------------------------------------------------------------
        // INTERNAL MODELS
        // --------------------------------------------------------------------
        private class FieldRectangle
        {
            public float X1 { get; set; }
            public float Y1 { get; set; }
            public float X2 { get; set; }
            public float Y2 { get; set; }
            public int PageNumber { get; set; }
            public string FieldType { get; set; }
        }

        private class TextBlock
        {
            public string Text { get; set; } = string.Empty;
            public float X1 { get; set; }
            public float Y1 { get; set; }
            public float X2 { get; set; }
            public float Y2 { get; set; }
            public int PageNumber { get; set; }
        }
    }
}
