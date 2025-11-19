using Consist.Doxi.Domain.Models;
using Consist.Doxi.Domain.Models.ExternalAPI;
using Consist.MCPServer.DoxiAPIClient;
using Doxi.APIClient;
using Doxi.APIClient.Models;

namespace Consist.Doxi.MCPServer.Domain
{
    public class DoxiAPIWrapper
    {
        private readonly IContextInformation _contextInformation;
        private readonly IDoxiClientService _doxiClientService;


        
        public DoxiAPIWrapper(IContextInformation contextInformation,
            IDoxiClientService doxiClientService)
        {
            _contextInformation = contextInformation;
            _doxiClientService = doxiClientService;
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

       
    }
}
