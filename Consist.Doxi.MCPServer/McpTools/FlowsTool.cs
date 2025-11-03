using Consist.Doxi.Domain.Models;
using Consist.Doxi.Domain.Models.ExternalAPI;
using Consist.Doxi.MCPServer.Domain;
using Consist.MCPServer.DoxiAPIClient;
using Doxi.APIClient.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

using System.Collections.Generic;
using Consist.Doxi.External.Models.Models.ExternalAPI.Webhook;
using Consist.Doxi.Enums;

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

        // =========================================================
        // FLOW MANAGEMENT
        // =========================================================

        [McpServerTool(Name = "GetAllFlows"), Description("Retrieves all signing flows available for the current tenant (GET /flow).")]
        public async Task<string> GetAllFlows(string username, string password)
        {
            var result = await _doxiAPIWrapper.GetAllFlows(username, password);
            return ToJson(result);
        }

        [McpServerTool(Name = "AddSignFlow"), Description("Creates a new signing flow and uploads a document (POST /flow).")]
        public async Task<string> AddSignFlow(string username, string password, ExCreateFlowRequestBase createFlowJsonRequest, byte[] documentFile)
        {
            var result = await _doxiAPIWrapper.AddSignFlow(username, password, createFlowJsonRequest, documentFile);
            return ToJson(result);
        }

        [McpServerTool(Name = "EditSignFlow"), Description("Edits an existing signing flow (POST /flow/edit).")]
        public async Task<string> EditSignFlow(string username, string password, EditFlowRequest request)
        {
            var result = await _doxiAPIWrapper.EditSignFlow(username, password, request);
            return ToJson(result);
        }

        [McpServerTool(Name = "GetDocument"), Description("Downloads a document from a signing flow (GET /flow/{signFlowId}/Document).")]
        public async Task<byte[]> GetDocument(string username, string password, string signFlowId, bool withSigns = true)
        {
            return await _doxiAPIWrapper.GetDocument(username, password, signFlowId, withSigns);
        }

        [McpServerTool(Name = "GetFlow"), Description("Retrieves metadata for a specific signing flow (GET /flow/{signFlowId}).")]
        public async Task<string> GetFlow(string username, string password, string signFlowId)
        {
            var result = await _doxiAPIWrapper.GetFlow(username, password, signFlowId);
            return ToJson(result);
        }

        [McpServerTool(Name = "SearchFlow"), Description("Searches signing flows by filters such as signer, date, or status (POST /flow/search).")]
        public async Task<string> SearchFlow(string username, string password, GetFlowsByFilterRequest request)
        {
            var result = await _doxiAPIWrapper.SearchFlow(username, password, request);
            return ToJson(result);
        }

        [McpServerTool(Name = "GetFlowsStatus"), Description("Retrieves statuses for multiple flows at once (POST /flow/status).")]
        public async Task<string> GetFlowsStatus(string username, string password, GetFlowsStatusRequest request)
        {
            var result = await _doxiAPIWrapper.GetFlowsStatus(username, password, request);
            return ToJson(result);
        }

        [McpServerTool(Name = "GetFlowStatus"), Description("Gets the current status of a specific flow (GET /flow/{signFlowId}/status).")]
        public async Task<string> GetFlowStatus(string username, string password, string signFlowId)
        {
            var result = await _doxiAPIWrapper.GetFlowStatus(username, password, signFlowId);
            return ToJson(result);
        }

        [McpServerTool(Name = "SetFlowAction"), Description("Sets an action on a flow such as approve, reject, or delegate (POST /flow/{signFlowId}/action).")]
        public async Task SetFlowAction(string username, string password, string signFlowId, SetFlowActionRequest request)
        {
            await _doxiAPIWrapper.SetFlowAction(username, password, signFlowId, request);
        }

        [McpServerTool(Name = "SetSignatures"), Description("Updates or adds signatures to a flow (POST /flow/{signFlowId}/SetSignatures).")]
        public async Task SetSignatures(string username, string password, string signFlowId, ExSetSignFlowRequest request)
        {
            await _doxiAPIWrapper.SetSignatures(username, password, signFlowId, request);
        }

        [McpServerTool(Name = "ReplaceSigner"), Description("Replaces an existing signer in a signing flow (POST /flow/ReplaceSigner).")]
        public async Task ReplaceSigner(string username, string password, ExReplaceSignerRequest request)
        {
            await _doxiAPIWrapper.ReplaceSigner(username, password, request);
        }

        [McpServerTool(Name = "GetFlowAttachments"), Description("Gets all attachments of a flow (GET /flow/{signFlowId}/attachments).")]
        public async Task<byte[]> GetFlowAttachments(string username, string password, string signFlowId)
        {
            return await _doxiAPIWrapper.GetFlowAttachments(username, password, signFlowId);
        }

        [McpServerTool(Name = "GetFlowAttachmentField"), Description("Gets a specific attachment field from a flow (POST /flow/{signFlowId}/AttachmentField).")]
        public async Task<byte[]> GetFlowAttachmentField(string username, string password, string signFlowId, GetFlowAttachmentFieldRequest request)
        {
            return await _doxiAPIWrapper.GetFlowAttachmentField(username, password, signFlowId, request);
        }

        [McpServerTool(Name = "AddAttachmentAsBase64ToFlow"), Description("Adds a base64-encoded attachment to a flow (POST /flow/{signFlowId}/attachments/base64).")]
        public async Task<string> AddAttachmentAsBase64ToFlow(string username, string password, string signFlowId, AddAttachmentBase64ToFlowRequest request)
        {
            var result = await _doxiAPIWrapper.AddAttachmentAsBase64ToFlow(username, password, signFlowId, request);
            return ToJson(result);
        }

        [McpServerTool(Name = "CreateFlowFromTemplate"), Description("Creates a new signing flow from a template (POST /ex/template/CreateFlowFromTemplate/{templateId}).")]
        public async Task<string> CreateFlowFromTemplate(string username, string password, string templateId, CreateFlowFromTemplateRequest request)
        {
            var result = await _doxiAPIWrapper.CreateFlowFromTemplate(username, password, templateId, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "AddTemplate"), Description("Creates a new user template (POST /ex/template).")]
        public async Task<string> AddTemplate(string username, string password, ExAddTemplateRequest request)
        {
            var result = await _doxiAPIWrapper.AddTemplate(username, password, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "UpdateTemplate"), Description("Updates an existing template (PUT /ex/template/{templateId}).")]
        public async Task UpdateTemplate(string username, string password, string templateId, ExUpdateTemplateRequest request)
        {
            await _doxiAPIWrapper.UpdateTemplate(username, password, templateId, request);
        }


        [McpServerTool(Name = "DeleteUserTemplate"), Description("Deletes a user template (DELETE /ex/template/{templateId}).")]
        public async Task DeleteUserTemplate(string username, string password, string templateId, DeleteTemplateRequest request)
        {
            await _doxiAPIWrapper.DeleteUserTemplate(username, password, templateId, request);
        }


        [McpServerTool(Name = "GetTemplate"), Description("Retrieves template metadata (GET /ex/template/{templateId}).")]
        public async Task<string> GetTemplate(string username, string password, string templateId)
        {
            var result = await _doxiAPIWrapper.GetTemplate(username, password, templateId);
            return ToJson(result);
        }


        [McpServerTool(Name = "DeleteAttachmentFromTemplate"), Description("Deletes an attachment from a template (DELETE /ex/template/{templateId}/attachments/{attachmentId}).")]
        public async Task DeleteAttachmentFromTemplate(string username, string password, string templateId, string attachmentId)
        {
            await _doxiAPIWrapper.DeleteAttachmentFromTemplate(username, password, templateId, attachmentId);
        }


        [McpServerTool(Name = "DocumentInfo"), Description("Extracts information from a document file (POST /ex/document/info).")]
        public async Task<string> DocumentInfo(string username, string password, byte[] document)
        {
            var result = await _doxiAPIWrapper.DocumentInfo(username, password, document);
            return ToJson(result);
        }


        [McpServerTool(Name = "DocumentInfoBase64"), Description("Extracts information from a document provided as base64 (POST /ex/document/info/base64).")]
        public async Task<string> DocumentInfoBase64(string username, string password, GetDocumentInfoRquest request)
        {
            var result = await _doxiAPIWrapper.DocumentInfoBase64(username, password, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "SearchInDocumentBase64"), Description("Searches for text or patterns in a base64-encoded document (POST /ex/document/search/base64).")]
        public async Task<string> SearchInDocumentBase64(string username, string password, SearchInDocumentBase64Request request)
        {
            var result = await _doxiAPIWrapper.SearchInDocumentBase64(username, password, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "SearchInDocument"), Description("Searches for text or patterns in an uploaded document (POST /ex/document/search).")]
        public async Task<string> SearchInDocument(string username, string password, byte[] file, SearchInDocumentRequest request)
        {
            var result = await _doxiAPIWrapper.SearchInDocument(username, password, file, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "MergeDocuments"), Description("Merges multiple documents into a single file (POST /ex/document/merge).")]
        public async Task<byte[]> MergeDocuments(string username, string password, IEnumerable<byte[]> documents)
        {
            return await _doxiAPIWrapper.MergeDocuments(username, password, documents);
        }


        [McpServerTool(Name = "AddKit"), Description("Creates a new kit (POST /ex/kit).")]
        public async Task<string> AddKit(string username, string password, ExAddKitRequest request)
        {
            var result = await _doxiAPIWrapper.AddKit(username, password, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "UpdateKit"), Description("Updates an existing kit (PUT /ex/kit/{kitId}).")]
        public async Task UpdateKit(string username, string password, string kitId, ExUpdateKitRequest request)
        {
            await _doxiAPIWrapper.UpdateKit(username, password, kitId, request);
        }


        [McpServerTool(Name = "GetKit"), Description("Retrieves kit metadata (GET /ex/kit/{kitId}).")]
        public async Task<string> GetKit(string username, string password, string kitId)
        {
            var result = await _doxiAPIWrapper.GetKit(username, password, kitId);
            return ToJson(result);
        }


        [McpServerTool(Name = "GetKits"), Description("Lists kits for the tenant (GET /ex/kit).")]
        public async Task<string> GetKits(string username, string password)
        {
            var result = await _doxiAPIWrapper.GetKits(username, password);
            return ToJson(result);
        }


        [McpServerTool(Name = "GetFormSettings"), Description("Downloads form settings for a company form (GET /ex/company/{companyId}/forms/{formId}/settings).")]
        public async Task<byte[]> GetFormSettings(string username, string password, string companyId, string formId)
        {
            return await _doxiAPIWrapper.GetFormSettings(username, password, companyId, formId);
        }


        [McpServerTool(Name = "GetUserGroups"), Description("Retrieves groups for a user identified by key type and value (GET /ex/user/{searchType}/{searchValue}/groups).")]
        public async Task<string> GetUserGroups(string username, string password, ParticipantKeyType searchType, string searchValue)
        {
            var result = await _doxiAPIWrapper.GetUserGroups(username, password, searchType, searchValue);
            return ToJson(result);
        }


        [McpServerTool(Name = "GetUserTemplates"), Description("Retrieves templates of a user identified by key type and value (GET /ex/user/{searchType}/{searchValue}/templates).")]
        public async Task<string> GetUserTemplates(string username, string password, ParticipantKeyType searchType, string searchValue)
        {
            var result = await _doxiAPIWrapper.GetUserTemplates(username, password, searchType, searchValue);
            return ToJson(result);
        }


        [McpServerTool(Name = "GetUserIdByEmail"), Description("Retrieves the user ID by email (GET /ex/user/byEmail/{email}/id).")]
        public async Task<string> GetUserIdByEmail(string username, string password, string email)
        {
            var result = await _doxiAPIWrapper.GetUserIdByEmail(username, password, email);
            return ToJson(result);
        }


        [McpServerTool(Name = "GetUsers"), Description("Queries users with filter parameters (GET /ex/users).")]
        public async Task<string> GetUsers(string username, string password, Dictionary<string, object> queryParams)
        {
            var result = await _doxiAPIWrapper.GetUsers(username, password, queryParams);
            return ToJson(result);
        }


        [McpServerTool(Name = "AddSubscription"), Description("Adds a new webhook subscription (POST /ex/webhook).")]
        public async Task<string> AddSubscription(string username, string password, WebhookSubscription request)
        {
            var result = await _doxiAPIWrapper.AddSubscription(username, password, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "WebHookCheck"), Description("Validates and previews a webhook payload (POST /ex/webhook/check).")]
        public async Task<string> WebHookCheck(string username, string password, WebhookSubscription request)
        {
            var result = await _doxiAPIWrapper.WebHookCheck(username, password, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "GetAllWebhookSubscription"), Description("Lists all webhook subscriptions (GET /ex/webhook).")]
        public async Task<string> GetAllWebhookSubscription(string username, string password)
        {
            var result = await _doxiAPIWrapper.GetAllWebhookSubscription(username, password);
            return ToJson(result);
        }


        [McpServerTool(Name = "SearchWebhookCallLogs"), Description("Searches webhook call logs for a subscription (POST /ex/webhook/{subscriptionId}/logs/search).")]
        public async Task<string> SearchWebhookCallLogs(string username, string password, string subscriptionId, RequestWebhookSenderLog request)
        {
            var result = await _doxiAPIWrapper.SearchWebhookCallLogs(username, password, subscriptionId, request);
            return ToJson(result);
        }


        [McpServerTool(Name = "UpdateWebhookSubscription"), Description("Updates a webhook subscription (PUT /ex/webhook/{subscriptionId}).")]
        public async Task UpdateWebhookSubscription(string username, string password, string subscriptionId, WebhookSubscription request)
        {
            await _doxiAPIWrapper.UpdateWebhookSubscription(username, password, subscriptionId, request);
        }


        [McpServerTool(Name = "DeleteSubscription"), Description("Deletes a webhook subscription (DELETE /ex/webhook/{subscriptionId}).")]
        public async Task DeleteSubscription(string username, string password, string subscriptionId)
        {
            await _doxiAPIWrapper.DeleteSubscription(username, password, subscriptionId);
        }
    }
    
}