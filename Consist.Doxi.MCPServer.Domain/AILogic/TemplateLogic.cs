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
                 templateInformationFromPrompt = await _aIModelDataExtractionClient.ExtractTemplateInformationFromPrompt(templateInstructions);
            var templateInformationFromPDF = await _aIModelDataExtractionClient.ExtractTemplateInformationFromPDF(templateDocument);
            var documentFields = await _documentFieldExtractor.GetDocumentElements(templateDocument);

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
            return null;
            //var signers = createTemplateInformation.Signers.ToArray();
            //return new ExAddTemplateRequest
            //{
            //    DocumentFileName = "tempalte.pdf",
            //    Base64DocumentFile = Convert.ToBase64String(templateDocument),
            //    TemplateName = templateInformationFromPrompt.TemplateName ?? templateInformationFromPDF.TemplateName,
            //    FlowElements = documentFields.ToArray(),
            //    SendMethodType = (SendMethodType)templateInformationFromPrompt.SendMethodType
            //                        .GetValueOrDefault(
            //                            templateInformationFromPDF.SendMethodType
            //                                .GetValueOrDefault(0)
            //                        ),
            //    SenderKey = new ParticipantKey<ParticipantKeyType>
            //    {
            //        Type = ParticipantKeyType.UserEmail,
            //        Key = createTemplateInformation.SenderEmail
            //    },
            //    TemplateType = TemplateType.Standard,//TODO: get by CreateTemplateInformation
            //    Users = createTemplateInformation.Signers.Select((s, index) => new ExTemplateUser
            //    {
            //        UserIndex = index,
            //        FixedSignerKey = GetFixedSignerKey(s)
            //    }).ToArray()
            //};
        }

        private ParticipantKey<ParticipantKeyType> GetFixedSignerKey(SignerInfo signerInfo)
        {
            if (!signerInfo.SignerType.HasValue || signerInfo.SignerType.Value != (int)SignerType.Static)
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
