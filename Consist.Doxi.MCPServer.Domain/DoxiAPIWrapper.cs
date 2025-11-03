using Consist.Doxi.Domain.Models;
using Consist.Doxi.Domain.Models.ExternalAPI;
using Consist.MCPServer.DoxiAPIClient;
using Consist.Doxi.External.Models.Models.ExternalAPI.Webhook;
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

        public async Task<CreateFlowResponse> AddSignFlow(string username, string password,
            ExCreateFlowRequestBase createFlowJsonRequest, byte[] documentFile)
            => await GetDoxiClient(username, password).AddSignFlow(createFlowJsonRequest, documentFile);

        public async Task<string> EditSignFlow(string username, string password, EditFlowRequest editFlowRequest)
            => await GetDoxiClient(username, password).EditSignFlow(editFlowRequest);

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

        public async Task SetSignatures(string username, string password, string signFlowId, ExSetSignFlowRequest exSetSignFlowRequest)
            => await GetDoxiClient(username, password).SetSignatures(signFlowId, exSetSignFlowRequest);

        public async Task ReplaceSigner(string username, string password, ExReplaceSignerRequest exReplaceSignerRequest)
            => await GetDoxiClient(username, password).ReplaceSigner(exReplaceSignerRequest);

        public async Task<byte[]> GetFlowAttachments(string username, string password, string signFlowId)
            => await GetDoxiClient(username, password).GetFlowAttachments(signFlowId);

        public async Task<byte[]> GetFlowAttachmentField(string username, string password, string signFlowId, GetFlowAttachmentFieldRequest request)
            => await GetDoxiClient(username, password).GetFlowAttachmentField(signFlowId, request);

        //public async Task<string> AddAttachmentToFlow(string username, string password, Doxi.APIClient.AddAttachmentToFlowRequest addAttachmentToFlowRequest)
        //    => await GetDoxiClient(username, password).AddAttachmentToFlow(addAttachmentToFlowRequest);

        public async Task<AddAttachmentAsBase64ToFlowResponse> AddAttachmentAsBase64ToFlow(string username, string password, string signFlowId, AddAttachmentBase64ToFlowRequest addAttachmentToFlowRequest)
            => await GetDoxiClient(username, password).AddAttachmentAsBase64ToFlow(signFlowId, addAttachmentToFlowRequest);

        // --------------------------------------------------------------------
        // TEMPLATE MANAGEMENT
        // --------------------------------------------------------------------

        public async Task<CreateFlowFromTemplateResponse> CreateFlowFromTemplate(string username, string password, string templateId, CreateFlowFromTemplateRequest request)
            => await GetDoxiClient(username, password).CreateFlowFromTemplate(templateId, request);

        public async Task<string> AddTemplate(string username, string password, ExAddTemplateRequest request)
            => await GetDoxiClient(username, password).AddTemplate(request);

        public async Task UpdateTemplate(string username, string password, string templateId, ExUpdateTemplateRequest request)
            => await GetDoxiClient(username, password).UpdateTemplate(templateId, request);

        public async Task DeleteUserTemplate(string username, string password, string templateId, DeleteTemplateRequest request)
            => await GetDoxiClient(username, password).DeleteUserTemplate(templateId, request);

        public async Task<GetExTemplateInfoResponse> GetTemplate(string username, string password, string templateId)
            => await GetDoxiClient(username, password).GetTemplate(templateId);

        //public async Task<string> AddAttachmentToTemplate(string username, string password, string templateId, Doxi.APIClient.AddAttachmentToFlowRequest request)
        //    => await GetDoxiClient(username, password).AddAttachmentToTemplate(templateId, request);

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
        // COMPANY
        // --------------------------------------------------------------------

        public async Task<byte[]> GetFormSettings(string username, string password, string companyId, string formId)
            => await GetDoxiClient(username, password).GetFormSettings(companyId, formId);

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

        // --------------------------------------------------------------------
        // WEBHOOKS
        // --------------------------------------------------------------------

        public async Task<AddWebHookSubscriptionResponse> AddSubscription(string username, string password, WebhookSubscription request)
            => await GetDoxiClient(username, password).AddSubscription(request);

        public async Task<WebhookPayload> WebHookCheck(string username, string password, WebhookSubscription request)
            => await GetDoxiClient(username, password).WebHookCheck(request);

        public async Task<IEnumerable<GetWebhookSubscriptionsResponse>> GetAllWebhookSubscription(string username, string password)
            => await GetDoxiClient(username, password).GetAllWebhookSubscription();

        public async Task<IEnumerable<SearchWebhookCallLogsResponse>> SearchWebhookCallLogs(string username, string password, string subscriptionId, RequestWebhookSenderLog request)
            => await GetDoxiClient(username, password).SearchWebhookCallLogs(subscriptionId, request);

        public async Task UpdateWebhookSubscription(string username, string password, string subscriptionId, WebhookSubscription request)
            => await GetDoxiClient(username, password).UpdateWebhookSubscription(subscriptionId, request);

        public async Task DeleteSubscription(string username, string password, string subscriptionId)
            => await GetDoxiClient(username, password).DeleteSubscription(subscriptionId);
    }
}
