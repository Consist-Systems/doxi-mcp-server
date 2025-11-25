using ApryseDataExtractor;
using Consist.Doxi.Domain.Models;
using Consist.Doxi.Enums;
using Consist.Doxi.MCPServer.Domain.Models;
using Consist.GPTDataExtruction;
using Consist.GPTDataExtruction.Model;
using Consist.MCPServer.DoxiAPIClient;
using Consist.PDFConverter;
using Doxi.APIClient;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;

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
            var templateInformationFromPDF = await _aIModelDataExtractionClient.ExtractTemplateInformationFromPDF(templateDocument);
            var documentFields = await _documentFieldExtractor.GetDocumentElements(templateDocument, templateInformationFromPDF.Languages);
            
            documentFields = FixOverlappingFields(documentFields);

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
                TemplateName = templateInformationFromPrompt.TemplateName ?? templateInformationFromPDF.TemplateName,
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

        private IEnumerable<ExTemplatFlowElement> FixOverlappingFields(IEnumerable<ExTemplatFlowElement> fields)
        {
            var result = new List<ExTemplatFlowElement>();
            var grouped = fields.GroupBy(f => f.PageNumber);

            foreach (var group in grouped)
            {
                var pageFields = group.ToList();
                bool changed = true;
                while (changed)
                {
                    changed = false;
                    for (int i = 0; i < pageFields.Count; i++)
                    {
                        for (int j = i + 1; j < pageFields.Count; j++)
                        {
                            var f1 = pageFields[i];
                            var f2 = pageFields[j];

                            if (IsOverlapping(f1, f2))
                            {
                                // Merge
                                var merged = MergeFields(f1, f2);
                                pageFields.RemoveAt(j);
                                pageFields.RemoveAt(i);
                                pageFields.Add(merged);
                                changed = true;
                                break;
                            }
                        }
                        if (changed) break;
                    }
                }
                result.AddRange(pageFields);
            }
            return result;
        }

        private bool IsOverlapping(ExTemplatFlowElement f1, ExTemplatFlowElement f2)
        {
            var r1 = new RectangleF((float)f1.Position.Left, (float)f1.Position.Top, (float)f1.Position.Width, (float)f1.Position.Height);
            var r2 = new RectangleF((float)f2.Position.Left, (float)f2.Position.Top, (float)f2.Position.Width, (float)f2.Position.Height);

            if (!r1.IntersectsWith(r2)) return false;

            var intersection = RectangleF.Intersect(r1, r2);
            var area1 = r1.Width * r1.Height;
            var area2 = r2.Width * r2.Height;
            var intersectArea = intersection.Width * intersection.Height;

            // If intersection is significant (e.g. > 20% of the smaller field)
            if (intersectArea > 0.2 * Math.Min(area1, area2))
                return true;

            return false;
        }

        private ExTemplatFlowElement MergeFields(ExTemplatFlowElement f1, ExTemplatFlowElement f2)
        {
            var r1 = new RectangleF((float)f1.Position.Left, (float)f1.Position.Top, (float)f1.Position.Width, (float)f1.Position.Height);
            var r2 = new RectangleF((float)f2.Position.Left, (float)f2.Position.Top, (float)f2.Position.Width, (float)f2.Position.Height);
            var union = RectangleF.Union(r1, r2);

            return new ExTemplatFlowElement
            {
                ElementId = f1.ElementId, // Keep one ID
                PageNumber = f1.PageNumber,
                Position = new ElementPosition
                {
                    Left = union.X,
                    Top = union.Y,
                    Width = union.Width,
                    Height = union.Height
                },
                ElementLabel = f1.ElementLabel ?? f2.ElementLabel,
                ElementType = f1.ElementType // Assume same type or take first
            };
        }

        private async Task UpdateFieldLabelsAndSigners(IEnumerable<ExTemplatFlowElement> documentFields, TemplateInfoFromPDFwithFields templateInformationFromPDF, byte[] templateDocument)
        {
            var images = await _documentConverter.PDFToImages(templateDocument);
            var imageList = images.ToList();

            var fieldsByPage = documentFields.GroupBy(f => f.PageNumber);
            var labeledImages = new List<byte[]>();
            var fieldMap = new Dictionary<int, ExTemplatFlowElement>();
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
                var font = new Font("Arial", 24, FontStyle.Bold);
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

            var prompt = "I have marked fields with red rectangles and numbers. " +
                         "For each number, please provide a unique label and estimate the signer from the following list: " +
                         string.Join(", ", templateInformationFromPDF.Signers.Select(s => s.Title)) +
                         ". Return JSON with list of { FieldNumber, Label, Signer }.";

            _genericGptClient.SetModelData(model: "gpt-4o");
            var gptResponse = await _genericGptClient.RunModelByFiles<GptResponse>(labeledImages, prompt);

            if (gptResponse?.Fields != null)
            {
                foreach (var item in gptResponse.Fields)
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
