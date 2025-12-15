using ApryseDataExtractor;
using Consist.Doxi.Domain.Models;
using Consist.Doxi.Enums;
using Consist.Doxi.MCPServer.Domain.Models;
using Consist.GPTDataExtruction;
using Consist.GPTDataExtruction.Model;
using Consist.MCPServer.DoxiAPIClient;
using Consist.PDFTools;
using Doxi.APIClient;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;

namespace Consist.Doxi.MCPServer.Domain.AILogic
{
    public class TemplateLogic
    {
        private readonly IDocumentFieldExtractor _documentFieldExtractor;
        private readonly IAIModelDataExtractionClient _aIModelDataExtractionClient;
        private readonly IDoxiClientService _doxiClientService;
        private readonly IContextInformation _contextInformation;
        private readonly IDocumentConverter _documentConverter;
        private readonly GenericGptClient _genericGptClient;

        public TemplateLogic(IDocumentFieldExtractor documentFieldExtractor,
            IAIModelDataExtractionClient aIModelDataExtractionClient,
            IDoxiClientService doxiClientService,
            IContextInformation contextInformation,
            IDocumentConverter documentConverter,
            GenericGptClient genericGptClient)
        {
            _documentFieldExtractor = documentFieldExtractor;
            _aIModelDataExtractionClient = aIModelDataExtractionClient;
            _doxiClientService = doxiClientService;
            _contextInformation = contextInformation;
            _documentConverter = documentConverter;
            _genericGptClient = genericGptClient;
        }

        public async Task<AddTemplateResponse> AddTemplate(string username, string password, byte[] templateDocument, string templateInstructions)
        {
            CreateTemplateInformation templateInformationFromPrompt=null;
            templateInformationFromPrompt = await _aIModelDataExtractionClient.ExtractTemplateInformationFromPrompt(templateInstructions);
            var documentPagesAsImages = await _documentConverter.PDFToImages(templateDocument);
            var templateInformationFromPDF = await _aIModelDataExtractionClient.ExtractTemplateInformationFromPDF(documentPagesAsImages);
            var documentFields = await _documentFieldExtractor.GetDocumentElements(templateDocument, templateInformationFromPDF.Languages);
            
            documentFields = FixOverlappingFields(documentFields.ToList());

            await UpdateFieldLabelsAndSigners(documentFields, templateInformationFromPDF, templateDocument);

            var exAddTemplateRequest = GetExAddTemplateRequest(templateInformationFromPrompt, templateInformationFromPDF, documentFields, templateDocument);
            var doxiClient = GetDoxiClient(username, password);
            var debugRequest = JsonConvert.SerializeObject(exAddTemplateRequest);
            var templateId = await doxiClient.AddTemplate(exAddTemplateRequest);
            return new AddTemplateResponse { TemplateId = templateId };
        }

        private DoxiClient GetDoxiClient(string username, string password)
        {
            var doxiClientContext = new DoxiClientContext
            {
                Username = username,
                Password = password,
                Tenant = _contextInformation.Tenant,
            };
            return _doxiClientService[doxiClientContext];
        }

        private ExAddTemplateRequest GetExAddTemplateRequest(CreateTemplateInformation templateInformationFromPrompt, TemplateInfoFromPDFwithFields templateInformationFromPDF, IEnumerable<ExTemplatFlowElement> documentFields, byte[] templateDocument)
        {
            if (templateInformationFromPrompt == null)
                templateInformationFromPrompt = new CreateTemplateInformation();

            var result = new ExAddTemplateRequest
            {
                DocumentFileName = "tempalte.pdf",
                Base64DocumentFile = Convert.ToBase64String(templateDocument),
                TemplateName = string.IsNullOrWhiteSpace(templateInformationFromPrompt.TemplateName) ? templateInformationFromPDF.TemplateName: templateInformationFromPrompt.TemplateName,
                SendMethodType = (SendMethodType)templateInformationFromPrompt.SendMethodType
                                    .GetValueOrDefault(
                                        templateInformationFromPDF.SendMethodType
                                            .GetValueOrDefault(0)
                                    ),
                SenderKey = new ParticipantKey<ParticipantKeyType>
                {
                    Type = ParticipantKeyType.UserEmail,
                    Key = templateInformationFromPrompt.SenderEmail
                }
            };

            var signers = GetSigners(templateInformationFromPrompt, templateInformationFromPDF);
            result.Users = signers.Select((s, index) => new ExTemplateUser
            {
                UserIndex = index,
                Title = s.Title,
                SignerType = (SignerType)s.SignerType.GetValueOrDefault(0),
                FixedSignerKey = GetFixedSignerKey(templateInformationFromPrompt?.Signers?.FirstOrDefault(si => si.Title == s.Title)),
            }).ToArray();
            result.FlowElements = GetAddTemplateRequestFlowElements(documentFields, signers).ToArray();

            return result;
        }

        private IEnumerable<ExTemplatFlowElement> GetAddTemplateRequestFlowElements(IEnumerable<ExTemplatFlowElement> documentFields, IEnumerable<SignerData> signers)
        {
            for (int signerIndex = 0; signerIndex < signers.Count(); signerIndex++)
            {
                var signer = signers.ElementAt(signerIndex);

                foreach (var field in documentFields)
                {
                    if (signer.Fields.Contains(field.ElementLabel))
                    {
                        field.UserIndex = signerIndex;
                        yield return field;
                    }
                    else //field not found
                    {
                        field.UserIndex = 0;
                        yield return field;
                    }
                }
            }
        }

        private IEnumerable<SignerData> GetSigners(CreateTemplateInformation templateInformationFromPrompt, TemplateInfoFromPDFwithFields templateInformationFromPDF)
        {
            if (templateInformationFromPrompt.Signers != null && templateInformationFromPrompt.Signers.Any())
            {
                var signersFromPromptResult = new List<SignerData>();
                foreach (var signer in templateInformationFromPrompt.Signers)
                {
                    var signerFromPDFInfo = templateInformationFromPDF.Signers.FirstOrDefault(s => s.Title == signer.Title);
                    if (signerFromPDFInfo != null)
                    {
                        var fixedSignerKey = GetFixedSignerKey(signer);
                        signersFromPromptResult.Add(new SignerData
                        {
                            Title = signer.Title,
                            SignerType = signer.SignerType,
                            Fields = signerFromPDFInfo.Fields,
                            FixedSignerKey = fixedSignerKey
                        });
                    }
                }
                if (signersFromPromptResult.Any())
                    return signersFromPromptResult;
            }

            return templateInformationFromPDF.Signers.Select(s => SignerData.ConvertToSignerData(s));
        }


        private IEnumerable<ExTemplatFlowElement> FixOverlappingFields(List<ExTemplatFlowElement> fields)
        {
            bool changed = false;
            var result = new List<ExTemplatFlowElement>();
            var grouped = fields.GroupBy(f => f.PageNumber);

            foreach (var group in grouped)
            {
                changed = FixPageOverlappingFieldsInternal(changed, result, group);
            }
           
            return result;

        }

        private bool FixPageOverlappingFieldsInternal(bool changed, List<ExTemplatFlowElement> result, IGrouping<int, ExTemplatFlowElement> group)
        {
            var pageFields = group.ToList();
            bool localChanged = true;

            while (localChanged)
            {
                localChanged = false;

                for (int i = 0; i < pageFields.Count; i++)
                {
                    for (int j = i + 1; j < pageFields.Count; j++)
                    {
                        var f1 = pageFields[i];
                        var f2 = pageFields[j];

                        if (IsOverlapping(f1, f2))
                        {
                            var merged = MergeFields(f1, f2);

                            // Remove higher index first
                            if (j > i)
                            {
                                pageFields.RemoveAt(j);
                                pageFields.RemoveAt(i);
                            }
                            else
                            {
                                pageFields.RemoveAt(i);
                                pageFields.RemoveAt(j);
                            }

                            pageFields.Add(merged);

                            localChanged = true;
                            changed = true;
                            break; // restart scanning
                        }
                    }

                    if (localChanged)
                        break;
                }
            }

            result.AddRange(pageFields);
            return changed;
        }

        private bool IsOverlapping(ExTemplatFlowElement f1, ExTemplatFlowElement f2)
        {
            if (f1.ElementId == f2.ElementId)
                return false;

            var r1 = new RectangleF((float)f1.Position.Left, (float)f1.Position.Top, (float)f1.Position.Width, (float)f1.Position.Height);
            var r2 = new RectangleF((float)f2.Position.Left, (float)f2.Position.Top, (float)f2.Position.Width, (float)f2.Position.Height);

            if (!r1.IntersectsWith(r2))
                return false;

            var intersection = RectangleF.Intersect(r1, r2);
            var area1 = r1.Width * r1.Height;
            var area2 = r2.Width * r2.Height;
            var intersectArea = intersection.Width * intersection.Height;

            return intersectArea > 0.2 * Math.Min(area1, area2);
        }

        private ExTemplatFlowElement MergeFields(ExTemplatFlowElement f1, ExTemplatFlowElement f2)
        {
            var r1 = new RectangleF((float)f1.Position.Left, (float)f1.Position.Top, (float)f1.Position.Width, (float)f1.Position.Height);
            var r2 = new RectangleF((float)f2.Position.Left, (float)f2.Position.Top, (float)f2.Position.Width, (float)f2.Position.Height);
            var union = RectangleF.Union(r1, r2);

            return new ExTemplatFlowElement
            {
                ElementId = f1.ElementId,
                PageNumber = f1.PageNumber,
                Position = new ElementPosition
                {
                    Left = union.X,
                    Top = union.Y,
                    Width = union.Width,
                    Height = union.Height
                },
                ElementLabel = f1.ElementLabel ?? f2.ElementLabel,
                ElementType = f1.ElementType
            };
        }


        private async Task UpdateFieldLabelsAndSigners(IEnumerable<ExTemplatFlowElement> documentFields, TemplateInfoFromPDFwithFields templateInformationFromPDF, byte[] templateDocument)
        {
            var images = await _documentConverter.PDFToImages(templateDocument);
            List<byte[]> labeledImages;
            Dictionary<int, ExTemplatFlowElement> fieldMap;
            GetLabledImages(documentFields, images, out labeledImages, out fieldMap);
            var fieldsPredictions = await _aIModelDataExtractionClient.GetFieldsPredictionsFromImages(documentFields, templateInformationFromPDF, labeledImages);
            UpdateFieldLabelsAndSigners(templateInformationFromPDF, fieldMap, fieldsPredictions);
        }

        private static void UpdateFieldLabelsAndSigners(TemplateInfoFromPDFwithFields templateInformationFromPDF, Dictionary<int, ExTemplatFlowElement> fieldMap, FieldsPredictions fieldsPredictions)
        {
            if (fieldsPredictions?.Fields != null)
            {
                foreach (var item in fieldsPredictions.Fields)
                {
                    if (fieldMap.TryGetValue(item.FieldNumber, out var field))
                    {
                        field.ElementLabel = item.Label;

                        var signer = templateInformationFromPDF.Signers.FirstOrDefault(s => s.Title == item.Signer);
                        if (signer != null)
                        {
                            var currentFields = signer.Fields?.ToList() ?? new List<string>();
                            if (!currentFields.Contains(item.Label))
                            {
                                currentFields.Add(item.Label);
                                signer.Fields = currentFields.ToArray();
                            }
                        }
                    }
                }
            }
        }

        private static void GetLabledImages(IEnumerable<ExTemplatFlowElement> documentFields, IEnumerable<byte[]> images, out List<byte[]> labeledImages, out Dictionary<int, ExTemplatFlowElement> fieldMap)
        {
            var imageList = images.ToList();

            var fieldsByPage = documentFields.GroupBy(f => f.PageNumber);
            labeledImages = new List<byte[]>();
            fieldMap = new Dictionary<int, ExTemplatFlowElement>();
            int fieldCounter = 1;

            // Scale factor: PDF points (72 DPI) to Image (assume 96 DPI default)
            // 96 / 72 = 1.3333f
            float scale = 1.3333f;

            for (int i = 0; i < imageList.Count; i++)
            {
                var pageNum = i + 1;
                var imageBytes = imageList[i];

                using var ms = new MemoryStream(imageBytes);
                using var bitmap = new Bitmap(ms);
                using var graphics = Graphics.FromImage(bitmap);

                var pen = new Pen(Color.Red, 3);
                var font = new Font("Arial", 8, FontStyle.Bold);
                var brush = new SolidBrush(Color.Red);

                if (fieldsByPage.Any(g => g.Key == pageNum))
                {
                    foreach (var field in fieldsByPage.First(g => g.Key == pageNum))
                    {
                        var rect = new Rectangle(
                            (int)(field.Position.Left * scale),
                            (int)(field.Position.Top * scale),
                            (int)(field.Position.Width * scale),
                            (int)(field.Position.Height * scale)
                        );

                        graphics.DrawRectangle(pen, rect);

                        var number = fieldCounter.ToString();
                        var textSize = graphics.MeasureString(number, font);
                        var textX = rect.X + (rect.Width - textSize.Width) / 2;
                        var textY = rect.Y + (rect.Height - textSize.Height) / 2;

                        graphics.FillRectangle(Brushes.White, textX, textY, textSize.Width, textSize.Height);
                        graphics.DrawString(number, font, brush, textX, textY);

                        fieldMap.Add(fieldCounter, field);
                        fieldCounter++;
                    }
                }

                using var outMs = new MemoryStream();
                bitmap.Save(outMs, ImageFormat.Png);
                labeledImages.Add(outMs.ToArray());
            }
        }

        private void DebugImages(List<byte[]> labeledImages)
        {
            for (int i = 0; i < labeledImages.Count; i++)
            {
                var imageName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"{i}.png");
                if (File.Exists(imageName))
                    File.Delete(imageName);
                File.WriteAllBytes(imageName, labeledImages[i]);
            }
        }

        private class GptResponse
        {
            public List<FieldPrediction> Fields { get; set; }
        }

        private class FieldPrediction
        {
            public int FieldNumber { get; set; }
            public string Label { get; set; }
            public string Signer { get; set; }
        }

        class SignerData : SignerResponseWithFields
        {
            public ParticipantKey<ParticipantKeyType> FixedSignerKey { get; set; }

            public static SignerData ConvertToSignerData(SignerResponseWithFields src)
            {
                var json = JsonConvert.SerializeObject(src);
                var result = JsonConvert.DeserializeObject<SignerData>(json);
                return result;
            }
        }

        private ParticipantKey<ParticipantKeyType> GetFixedSignerKey(SignerInfo signerInfo)
        {
            if (signerInfo == null || !signerInfo.SignerType.HasValue || signerInfo.SignerType.Value != (int)SignerType.Static)
                return null;

            if (signerInfo.FixedSigner == null)
                return null;
            if (!string.IsNullOrEmpty(signerInfo.FixedSigner.Email))
            {
                return new ParticipantKey<ParticipantKeyType>
                {
                    Type = ParticipantKeyType.UserEmail,
                    Key = signerInfo.FixedSigner.Email
                };
            }
            else if (!string.IsNullOrEmpty(signerInfo.FixedSigner.PhoneNumber))
            {
                return new ParticipantKey<ParticipantKeyType>
                {
                    Type = ParticipantKeyType.UserPhone,
                    Key = signerInfo.FixedSigner.PhoneNumber
                };
            }
            return null;
        }
    }
}
