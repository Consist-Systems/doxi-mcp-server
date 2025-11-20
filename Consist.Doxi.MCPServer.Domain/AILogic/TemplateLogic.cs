using ApryseDataExtractor;
using Consist.Doxi.Domain.Models;
using Consist.Doxi.Enums;
using Consist.Doxi.MCPServer.Domain.Models;
using Consist.GPTDataExtruction;
using Consist.GPTDataExtruction.Model;
using Consist.MCPServer.DoxiAPIClient;
using Doxi.APIClient;
using Newtonsoft.Json;

namespace Consist.Doxi.MCPServer.Domain.AILogic
{
    public class TemplateLogic
    {
        private readonly IDocumentFieldExtractor _documentFieldExtractor;
        private readonly IAIModelDataExtractionClient _aIModelDataExtractionClient;
        private readonly IDoxiClientService _doxiClientService;
        private readonly IContextInformation _contextInformation;

        public TemplateLogic(IDocumentFieldExtractor documentFieldExtractor,
            IAIModelDataExtractionClient aIModelDataExtractionClient,
            IDoxiClientService doxiClientService,
            IContextInformation contextInformation)
        {
            _documentFieldExtractor = documentFieldExtractor;
            _aIModelDataExtractionClient = aIModelDataExtractionClient;
            _doxiClientService = doxiClientService;
            _contextInformation = contextInformation;
        }

        public async Task<AddTemplateResponse> AddTemplate(string username, string password, byte[] templateDocument, string templateInstructions)
        {
            CreateTemplateInformation templateInformationFromPrompt=null;
            if (!string.IsNullOrWhiteSpace(templateInstructions))
                templateInformationFromPrompt = new CreateTemplateInformation
                {
                    SenderEmail = "ronenr@consist.co.il"
                };
            //templateInformationFromPrompt = await _aIModelDataExtractionClient.ExtractTemplateInformationFromPrompt(templateInstructions);
            var templateInformationFromPDF = await _aIModelDataExtractionClient.ExtractTemplateInformationFromPDF(templateDocument);
            var documentFields = await _documentFieldExtractor.GetDocumentElements(templateDocument, templateInformationFromPDF.Languages);

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

            var result =  new ExAddTemplateRequest
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
            result.Users = signers.Select((s,index) => new ExTemplateUser
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
                foreach (var field in signer.Fields)
                {
                    var documentField = documentFields.FirstOrDefault(df => df.ElementLabel == field);
                    if (documentField != null)
                    {
                        documentField.UserIndex = signerIndex;
                        yield return documentField;
                    }
                }
            }
        }

        private IEnumerable<SignerData> GetSigners(CreateTemplateInformation templateInformationFromPrompt, TemplateInfoFromPDFwithFields templateInformationFromPDF)
        {
            if (templateInformationFromPrompt.Signers!= null && templateInformationFromPrompt.Signers.Any())
            {
                var signersFromPromptResult = new List<SignerData>();
                foreach (var signer in templateInformationFromPrompt.Signers)
                {
                    var signerFromPDFInfo = templateInformationFromPDF.Signers.FirstOrDefault(s => s.Title == signer.Title);
                    if(signerFromPDFInfo != null)
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
                if(signersFromPromptResult.Any())
                    return signersFromPromptResult;
            }
            
            return templateInformationFromPDF.Signers.Select(s=> SignerData.ConvertToSignerData(s));
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
