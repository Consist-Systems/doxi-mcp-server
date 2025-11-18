using ApryseDataExtractor;
using Consist.Doxi.Domain.Models;
using Consist.Doxi.Domain.Models.ExternalAPI;
using Consist.Doxi.Enums;
using Consist.Doxi.External.Models.Models.ExternalAPI.Webhook;
using Consist.Doxi.MCPServer.Domain.Models;
using Consist.GPTDataExtruction;
using Consist.GPTDataExtruction.Model;
using Consist.MCPServer.DoxiAPIClient;
using Doxi.APIClient;
using Doxi.APIClient.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Consist.Doxi.MCPServer.Domain
{
    public class DoxiAPIWrapper
    {
        private readonly IContextInformation _contextInformation;
        private readonly IDoxiClientService _doxiClientService;
        private readonly IServiceProvider _serviceProvider;


        private IGPTDataExtractionClient GPTDataExtractionClient => _serviceProvider.GetService<IGPTDataExtractionClient>();
        private IDocumentFieldExtractor DocumentFieldExtractor => _serviceProvider.GetService<IDocumentFieldExtractor>();
        
        public DoxiAPIWrapper(IContextInformation contextInformation,
            IDoxiClientService doxiClientService,
            IServiceProvider serviceProvider)
        {
            _contextInformation = contextInformation;
            _doxiClientService = doxiClientService;
            _serviceProvider = serviceProvider;
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

        // --------------------------------------------------------------------
        // FLOW MANAGEMENT
        // --------------------------------------------------------------------

        public async Task<GetAllFlowsResponse> GetAllFlows(string username, string password)
            => await GetDoxiClient(username, password).GetAllFlows();

        public async Task<byte[]> GetDocument(string username, string password, string signFlowId, bool withSigns = true)
            => await GetDoxiClient(username, password).GetDocument(signFlowId, withSigns);

        public async Task<GetFlowMetadataResponse> GetFlow(string username, string password, string signFlowId)
            => await GetDoxiClient(username, password).GetFlow(signFlowId);

        public async Task<IEnumerable<string>> SearchFlow(string username, string password, GetFlowsByFilterRequest getFlowsByFilterRequest)
            => await GetDoxiClient(username, password).SearchFlow(getFlowsByFilterRequest);

        public async Task<GetFlowsStatusResponse[]> GetFlowsStatus(string username, string password, GetFlowsStatusRequest getFlowsStatusRequest)
            => await GetDoxiClient(username, password).GetFlowsStatus(getFlowsStatusRequest);

        public async Task<GetFlowStatusResponse> GetFlowStatus(string username, string password, string signFlowId)
            => await GetDoxiClient(username, password).GetFlowStatus(signFlowId);

        public async Task SetFlowAction(string username, string password, string signFlowId, SetFlowActionRequest setFlowActionRequest)
            => await GetDoxiClient(username, password).SetFlowAction(signFlowId, setFlowActionRequest);


        public async Task<byte[]> GetFlowAttachments(string username, string password, string signFlowId)
            => await GetDoxiClient(username, password).GetFlowAttachments(signFlowId);

        public async Task<byte[]> GetFlowAttachmentField(string username, string password, string signFlowId, GetFlowAttachmentFieldRequest request)
            => await GetDoxiClient(username, password).GetFlowAttachmentField(signFlowId, request);


        public async Task<AddAttachmentAsBase64ToFlowResponse> AddAttachmentAsBase64ToFlow(string username, string password, string signFlowId, AddAttachmentBase64ToFlowRequest addAttachmentToFlowRequest)
            => await GetDoxiClient(username, password).AddAttachmentAsBase64ToFlow(signFlowId, addAttachmentToFlowRequest);

        // --------------------------------------------------------------------
        // TEMPLATE MANAGEMENT
        // --------------------------------------------------------------------

        public async Task<CreateFlowFromTemplateResponse> CreateFlowFromTemplate(string username, string password, string templateId, CreateFlowFromTemplateRequest request)
            => await GetDoxiClient(username, password).CreateFlowFromTemplate(templateId, request);

        public async Task DeleteUserTemplate(string username, string password, string templateId, DeleteTemplateRequest request)
            => await GetDoxiClient(username, password).DeleteUserTemplate(templateId, request);

        public async Task<GetExTemplateInfoResponse> GetTemplate(string username, string password, string templateId)
            => await GetDoxiClient(username, password).GetTemplate(templateId);


        public async Task DeleteAttachmentFromTemplate(string username, string password, string templateId, string attachmentId)
            => await GetDoxiClient(username, password).DeleteAttachmentFromTemplate(templateId, attachmentId);

        // --------------------------------------------------------------------
        // DOCUMENT MANAGEMENT
        // --------------------------------------------------------------------

        public async Task<GetDocumentInfoResponse> DocumentInfo(string username, string password, byte[] document)
            => await GetDoxiClient(username, password).DocumentInfo(document);

        public async Task<GetDocumentInfoResponse> DocumentInfoBase64(string username, string password, GetDocumentInfoRquest request)
            => await GetDoxiClient(username, password).DocumentInfoBase64(request);

        public async Task<SearchInDocumentResponse> SearchInDocumentBase64(string username, string password, SearchInDocumentBase64Request request)
            => await GetDoxiClient(username, password).SearchInDocumentBase64(request);

        public async Task<SearchInDocumentResponse> SearchInDocument(string username, string password, byte[] file, SearchInDocumentRequest request)
            => await GetDoxiClient(username, password).SearchInDocument(file, request);

        public async Task<byte[]> MergeDocuments(string username, string password, IEnumerable<byte[]> documents)
            => await GetDoxiClient(username, password).MergeDocuments(documents);

        // --------------------------------------------------------------------
        // KIT MANAGEMENT
        // --------------------------------------------------------------------

        public async Task<ExAddKitResponse> AddKit(string username, string password, ExAddKitRequest request)
            => await GetDoxiClient(username, password).AddKit(request);

        public async Task UpdateKit(string username, string password, string kitId, ExUpdateKitRequest request)
            => await GetDoxiClient(username, password).UpdateKit(kitId, request);

        public async Task<ExGetKitInfoResponse> GetKit(string username, string password, string kitId)
            => await GetDoxiClient(username, password).GetKit(kitId);

        public async Task<IEnumerable<ExGetKitsResponse>> GetKits(string username, string password)
            => await GetDoxiClient(username, password).GetKits();


        // --------------------------------------------------------------------
        // USER MANAGEMENT
        // --------------------------------------------------------------------

        public async Task<List<GetGroupsResponseWithUsersKey>> GetUserGroups(string username, string password, Consist.Doxi.Enums.ParticipantKeyType searchType, string searchValue)
            => await GetDoxiClient(username, password).GetUserGroups(searchType, searchValue);

        public async Task<GetUserTemplatesResponse[]> GetUserTemplates(string username, string password, Consist.Doxi.Enums.ParticipantKeyType searchType, string searchValue)
            => await GetDoxiClient(username, password).GetUserTemplates(searchType, searchValue);

        public async Task<string> GetUserIdByEmail(string username, string password, string email)
            => await GetDoxiClient(username, password).GetUserIdByEmail(email);

        public async Task<IEnumerable<User>> GetUsers(string username, string password, Dictionary<string, object> queryParams)
            => await GetDoxiClient(username, password).GetUsers(queryParams);

        public async Task<AddTemplateResponse> AddTemplate(string username, string password, byte[] templateDocument, string templateInstructions)
        {
            var createTemplateInformation = await GPTDataExtractionClient.ExtractTemplateInformation(templateInstructions);
            var documentFields = await DocumentFieldExtractor.GetDocumentElements(templateDocument);
            
            IEnumerable<FieldWithSigner> fieldLableToSignerMapping;
            if (createTemplateInformation.Signers.Count() == 1)
                fieldLableToSignerMapping = documentFields.Select(f=>new FieldWithSigner(f.PageNumber,f.ElementId,0));
            else
                fieldLableToSignerMapping = await GPTDataExtractionClient.ExtractFieldLableToSignerMapping(createTemplateInformation.Signers,
                    documentFields.Select(f => new FieldWithPage(f.PageNumber, f.ElementLabel)),
                    templateDocument);

            var exAddTemplateRequest = GetExAddTemplateRequest(createTemplateInformation, documentFields, fieldLableToSignerMapping, templateDocument);
            var templateId = await GetDoxiClient(username, password).AddTemplate(exAddTemplateRequest);
            return new AddTemplateResponse { TemplateId = templateId };
        }

        

        private ExAddTemplateRequest GetExAddTemplateRequest(CreateTemplateInformation createTemplateInformation, IEnumerable<ExTemplatFlowElement> documentFields, IEnumerable<FieldWithSigner> fieldLableToSignerMapping, byte[] templateDocument)
        {
            var signers = createTemplateInformation.Signers.ToArray();
            return new ExAddTemplateRequest
            {
                DocumentFileName = "tempalte.pdf",
                Base64DocumentFile = Convert.ToBase64String(templateDocument),
                TemplateName = createTemplateInformation.Name,
                FlowElements = documentFields.ToArray(),
                SendMethodType = Enums.SendMethodType.ParallelFlow,//TODO: get by CreateTemplateInformation
                SenderKey = new ParticipantKey<ParticipantKeyType>  
                {
                    Type = ParticipantKeyType.UserEmail,
                    Key = createTemplateInformation.SenderEmail
                },
                TemplateType = TemplateType.Standard,//TODO: get by CreateTemplateInformation
                Users = fieldLableToSignerMapping.Select(s=>new ExTemplateUser
                {
                    UserIndex = s.SignerIndex,
                    FixedSignerKey = GetFixedSignerKey(signers[s.SignerIndex])
                }).ToArray()
            };
        }

        private ParticipantKey<ParticipantKeyType> GetFixedSignerKey(SignerInfo signerInfo)
        {
            if(!signerInfo.SignerType.HasValue || signerInfo.SignerType.Value != (int)SignerType.Static)
                return null;
            
            if(signerInfo.FixedSigner == null)
                return null;
            if(!string.IsNullOrEmpty(signerInfo.FixedSigner.Email))
            {
                return new ParticipantKey<ParticipantKeyType>
                {
                    Type = ParticipantKeyType.UserEmail,
                    Key = signerInfo.FixedSigner.Email
                };
            }
            else if(!string.IsNullOrEmpty(signerInfo.FixedSigner.PhoneNumber))
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
