using Consist.Doxi.Domain.Models;
using Consist.Doxi.Domain.Models.ExternalAPI;
using Consist.Doxi.Enums;
using Consist.Doxi.External.Models.Models.ExternalAPI.Webhook;
using Consist.Doxi.MCPServer.Domain;
using Doxi.APIClient.Models;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace Consist.ProjectName.McpTools
{
    [McpServerToolType]
    public class FlowsTool
    {
        private readonly DoxiAPIWrapper _doxiAPIWrapper;

        public FlowsTool(DoxiAPIWrapper doxiAPIWrapper)
        {
            _doxiAPIWrapper = doxiAPIWrapper;
        }

        private static string ToJson(object obj)
            => JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });

        private static TextContent Success(object extra = null)
            => new TextContent(ToJson(new { success = true, data = extra }));

        // =========================================================
        // FLOW MANAGEMENT
        // =========================================================

        [McpServerTool(Name = "GetAllFlows")]
        public async Task<TextContent> GetAllFlows(string username, string password)
        {
            var result = await _doxiAPIWrapper.GetAllFlows(username, password);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "AddSignFlow")]
        public async Task<TextContent> AddSignFlow(string username, string password, ExCreateFlowRequestBase createFlowJsonRequest, byte[] documentFile)
        {
            var result = await _doxiAPIWrapper.AddSignFlow(username, password, createFlowJsonRequest, documentFile);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "EditSignFlow")]
        public async Task<TextContent> EditSignFlow(string username, string password, EditFlowRequest request)
        {
            var result = await _doxiAPIWrapper.EditSignFlow(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetDocument")]
        public async Task<DataContent> GetDocument(string username, string password, string signFlowId, bool withSigns = true)
        {
            var bytes = await _doxiAPIWrapper.GetDocument(username, password, signFlowId, withSigns);
            return new DataContent(bytes, "application/pdf");
        }

        [McpServerTool(Name = "GetFlow")]
        public async Task<TextContent> GetFlow(string username, string password, string signFlowId)
        {
            var result = await _doxiAPIWrapper.GetFlow(username, password, signFlowId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SearchFlow")]
        public async Task<TextContent> SearchFlow(string username, string password, GetFlowsByFilterRequest request)
        {
            var result = await _doxiAPIWrapper.SearchFlow(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetFlowsStatus")]
        public async Task<TextContent> GetFlowsStatus(string username, string password, GetFlowsStatusRequest request)
        {
            var result = await _doxiAPIWrapper.GetFlowsStatus(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetFlowStatus")]
        public async Task<TextContent> GetFlowStatus(string username, string password, string signFlowId)
        {
            var result = await _doxiAPIWrapper.GetFlowStatus(username, password, signFlowId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SetFlowAction")]
        public async Task<TextContent> SetFlowAction(string username, string password, string signFlowId, SetFlowActionRequest request)
        {
            await _doxiAPIWrapper.SetFlowAction(username, password, signFlowId, request);
            return Success(new { signFlowId });
        }

        [McpServerTool(Name = "SetSignatures")]
        public async Task<TextContent> SetSignatures(string username, string password, string signFlowId, ExSetSignFlowRequest request)
        {
            await _doxiAPIWrapper.SetSignatures(username, password, signFlowId, request);
            return Success(new { signFlowId });
        }

        [McpServerTool(Name = "ReplaceSigner")]
        public async Task<TextContent> ReplaceSigner(string username, string password, ExReplaceSignerRequest request)
        {
            await _doxiAPIWrapper.ReplaceSigner(username, password, request);
            return Success();
        }

        [McpServerTool(Name = "GetFlowAttachments")]
        public async Task<DataContent> GetFlowAttachments(string username, string password, string signFlowId)
        {
            var bytes = await _doxiAPIWrapper.GetFlowAttachments(username, password, signFlowId);
            return new DataContent(bytes, "application/octet-stream");
        }

        [McpServerTool(Name = "GetFlowAttachmentField")]
        public async Task<DataContent> GetFlowAttachmentField(string username, string password, string signFlowId, GetFlowAttachmentFieldRequest request)
        {
            var bytes = await _doxiAPIWrapper.GetFlowAttachmentField(username, password, signFlowId, request);
            return new DataContent(bytes, "application/octet-stream");
        }

        [McpServerTool(Name = "AddAttachmentAsBase64ToFlow")]
        public async Task<TextContent> AddAttachmentAsBase64ToFlow(string username, string password, string signFlowId, AddAttachmentBase64ToFlowRequest request)
        {
            var result = await _doxiAPIWrapper.AddAttachmentAsBase64ToFlow(username, password, signFlowId, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "CreateFlowFromTemplate")]
        public async Task<TextContent> CreateFlowFromTemplate(string username, string password, string templateId, CreateFlowFromTemplateRequest request)
        {
            var result = await _doxiAPIWrapper.CreateFlowFromTemplate(username, password, templateId, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "AddTemplate")]
        public async Task<TextContent> AddTemplate(string username, string password, ExAddTemplateRequest request)
        {
            var result = await _doxiAPIWrapper.AddTemplate(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "UpdateTemplate")]
        public async Task<TextContent> UpdateTemplate(string username, string password, string templateId, ExUpdateTemplateRequest request)
        {
            await _doxiAPIWrapper.UpdateTemplate(username, password, templateId, request);
            return Success(new { templateId });
        }

        [McpServerTool(Name = "DeleteUserTemplate")]
        public async Task<TextContent> DeleteUserTemplate(string username, string password, string templateId, DeleteTemplateRequest request)
        {
            await _doxiAPIWrapper.DeleteUserTemplate(username, password, templateId, request);
            return Success(new { templateId });
        }

        [McpServerTool(Name = "GetTemplate")]
        public async Task<TextContent> GetTemplate(string username, string password, string templateId)
        {
            var result = await _doxiAPIWrapper.GetTemplate(username, password, templateId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "DeleteAttachmentFromTemplate")]
        public async Task<TextContent> DeleteAttachmentFromTemplate(string username, string password, string templateId, string attachmentId)
        {
            await _doxiAPIWrapper.DeleteAttachmentFromTemplate(username, password, templateId, attachmentId);
            return Success(new { templateId, attachmentId });
        }

        [McpServerTool(Name = "DocumentInfo")]
        public async Task<TextContent> DocumentInfo(string username, string password, byte[] document)
        {
            var result = await _doxiAPIWrapper.DocumentInfo(username, password, document);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "DocumentInfoBase64")]
        public async Task<TextContent> DocumentInfoBase64(string username, string password, GetDocumentInfoRquest request)
        {
            var result = await _doxiAPIWrapper.DocumentInfoBase64(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SearchInDocumentBase64")]
        public async Task<TextContent> SearchInDocumentBase64(string username, string password, SearchInDocumentBase64Request request)
        {
            var result = await _doxiAPIWrapper.SearchInDocumentBase64(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SearchInDocument")]
        public async Task<TextContent> SearchInDocument(string username, string password, byte[] file, SearchInDocumentRequest request)
        {
            var result = await _doxiAPIWrapper.SearchInDocument(username, password, file, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "MergeDocuments")]
        public async Task<DataContent> MergeDocuments(string username, string password, IEnumerable<byte[]> documents)
        {
            var bytes = await _doxiAPIWrapper.MergeDocuments(username, password, documents);
            return new DataContent(bytes, "application/pdf");
        }

        [McpServerTool(Name = "AddKit")]
        public async Task<TextContent> AddKit(string username, string password, ExAddKitRequest request)
        {
            var result = await _doxiAPIWrapper.AddKit(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "UpdateKit")]
        public async Task<TextContent> UpdateKit(string username, string password, string kitId, ExUpdateKitRequest request)
        {
            await _doxiAPIWrapper.UpdateKit(username, password, kitId, request);
            return Success(new { kitId });
        }

        [McpServerTool(Name = "GetKit")]
        public async Task<TextContent> GetKit(string username, string password, string kitId)
        {
            var result = await _doxiAPIWrapper.GetKit(username, password, kitId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetKits")]
        public async Task<TextContent> GetKits(string username, string password)
        {
            var result = await _doxiAPIWrapper.GetKits(username, password);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetUserGroups")]
        public async Task<TextContent> GetUserGroups(string username, string password, ParticipantKeyType searchType, string searchValue)
        {
            var result = await _doxiAPIWrapper.GetUserGroups(username, password, searchType, searchValue);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetUserTemplates")]
        public async Task<TextContent> GetUserTemplates(string username, string password, ParticipantKeyType searchType, string searchValue)
        {
            var result = await _doxiAPIWrapper.GetUserTemplates(username, password, searchType, searchValue);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetUserIdByEmail")]
        public async Task<TextContent> GetUserIdByEmail(string username, string password, string email)
        {
            var result = await _doxiAPIWrapper.GetUserIdByEmail(username, password, email);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetUsers")]
        public async Task<TextContent> GetUsers(string username, string password, Dictionary<string, object> queryParams)
        {
            var result = await _doxiAPIWrapper.GetUsers(username, password, queryParams);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "AddSubscription")]
        public async Task<TextContent> AddSubscription(string username, string password, WebhookSubscription request)
        {
            var result = await _doxiAPIWrapper.AddSubscription(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "WebHookCheck")]
        public async Task<TextContent> WebHookCheck(string username, string password, WebhookSubscription request)
        {
            var result = await _doxiAPIWrapper.WebHookCheck(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetAllWebhookSubscription")]
        public async Task<TextContent> GetAllWebhookSubscription(string username, string password)
        {
            var result = await _doxiAPIWrapper.GetAllWebhookSubscription(username, password);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SearchWebhookCallLogs")]
        public async Task<TextContent> SearchWebhookCallLogs(string username, string password, string subscriptionId, RequestWebhookSenderLog request)
        {
            var result = await _doxiAPIWrapper.SearchWebhookCallLogs(username, password, subscriptionId, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "UpdateWebhookSubscription")]
        public async Task<TextContent> UpdateWebhookSubscription(string username, string password, string subscriptionId, WebhookSubscription request)
        {
            await _doxiAPIWrapper.UpdateWebhookSubscription(username, password, subscriptionId, request);
            return Success(new { subscriptionId });
        }

        [McpServerTool(Name = "DeleteSubscription")]
        public async Task<TextContent> DeleteSubscription(string username, string password, string subscriptionId)
        {
            await _doxiAPIWrapper.DeleteSubscription(username, password, subscriptionId);
            return Success(new { subscriptionId });
        }
    }
}
