using Consist.Doxi.Domain.Models;
using Consist.Doxi.Domain.Models.ExternalAPI;
using Consist.Doxi.Enums;
using Consist.Doxi.MCPServer.Domain;
using Consist.Doxi.MCPServer.Domain.AILogic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Consist.ProjectName.McpTools
{
    [McpServerToolType]
    public class FlowsTool
    {
        private readonly IServiceProvider _serviceProvider;
        private DoxiAPIWrapper DoxiAPIWrapper => _serviceProvider.GetService<DoxiAPIWrapper>();

        private TemplateLogic TemplateLogic => _serviceProvider.GetService<TemplateLogic>();

        public FlowsTool(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private static string ToJson(object obj)
            => JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });

        private static TextContent Success(object extra = null)
            => new TextContent(ToJson(new { success = true, data = extra }));

        // =========================================================
        // FLOW MANAGEMENT
        // =========================================================

        [McpServerTool(Name = "GetAllFlows"), Description("Retrieves all signing flows available for the current tenant (GET /flow).")]
        public async Task<TextContent> GetAllFlows(string username, string password)
        {
            var result = await DoxiAPIWrapper.GetAllFlows(username, password);
            return new TextContent(ToJson(result));
        }


        [McpServerTool(Name = "GetDocument"), Description("Downloads a document from a signing flow (GET /flow/{signFlowId}/Document).")]
        public async Task<DataContent> GetDocument(string username, string password, string signFlowId, bool withSigns = true)
        {
            var bytes = await DoxiAPIWrapper.GetDocument(username, password, signFlowId, withSigns);
            return new DataContent(bytes, "application/pdf");
        }

        [McpServerTool(Name = "GetFlow"), Description("Retrieves metadata for a specific signing flow (GET /flow/{signFlowId}).")]
        public async Task<TextContent> GetFlow(string username, string password, string signFlowId)
        {
            var result = await DoxiAPIWrapper.GetFlow(username, password, signFlowId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SearchFlow"), Description("Searches signing flows by filters such as signer, date, or status (POST /flow/search).")]
        public async Task<TextContent> SearchFlow(string username, string password, GetFlowsByFilterRequest request)
        {
            var result = await DoxiAPIWrapper.SearchFlow(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetFlowsStatus"), Description("Retrieves statuses for multiple flows at once (POST /flow/status).")]
        public async Task<TextContent> GetFlowsStatus(string username, string password, GetFlowsStatusRequest request)
        {
            var result = await DoxiAPIWrapper.GetFlowsStatus(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetFlowStatus"), Description("Gets the current status of a specific flow (GET /flow/{signFlowId}/status).")]
        public async Task<TextContent> GetFlowStatus(string username, string password, string signFlowId)
        {
            var result = await DoxiAPIWrapper.GetFlowStatus(username, password, signFlowId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SetFlowAction"), Description("Sets an action on a flow such as approve, reject, or delegate (POST /flow/{signFlowId}/action).")]
        public async Task<TextContent> SetFlowAction(string username, string password, string signFlowId, SetFlowActionRequest request)
        {
            await DoxiAPIWrapper.SetFlowAction(username, password, signFlowId, request);
            return Success(new { signFlowId });
        }

        [McpServerTool(Name = "GetFlowAttachments"), Description("Gets all attachments of a flow (GET /flow/{signFlowId}/attachments).")]
        public async Task<DataContent> GetFlowAttachments(string username, string password, string signFlowId)
        {
            var bytes = await DoxiAPIWrapper.GetFlowAttachments(username, password, signFlowId);
            return new DataContent(bytes, "application/octet-stream");
        }

        [McpServerTool(Name = "GetFlowAttachmentField"), Description("Gets a specific attachment field from a flow (POST /flow/{signFlowId}/AttachmentField).")]
        public async Task<DataContent> GetFlowAttachmentField(string username, string password, string signFlowId, GetFlowAttachmentFieldRequest request)
        {
            var bytes = await DoxiAPIWrapper.GetFlowAttachmentField(username, password, signFlowId, request);
            return new DataContent(bytes, "application/octet-stream");
        }

        [McpServerTool(Name = "AddAttachmentAsBase64ToFlow"), Description("Adds a base64-encoded attachment to a flow (POST /flow/{signFlowId}/attachments/base64).")]
        public async Task<TextContent> AddAttachmentAsBase64ToFlow(string username, string password, string signFlowId, AddAttachmentBase64ToFlowRequest request)
        {
            var result = await DoxiAPIWrapper.AddAttachmentAsBase64ToFlow(username, password, signFlowId, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "CreateFlowFromTemplate"), Description("Creates a new signing flow from a template (POST /ex/template/CreateFlowFromTemplate/{templateId}).")]
        public async Task<TextContent> CreateFlowFromTemplate(string username, string password, string templateId, CreateFlowFromTemplateRequest request)
        {
            var result = await DoxiAPIWrapper.CreateFlowFromTemplate(username, password, templateId, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "DeleteUserTemplate"), Description("Deletes a user template (DELETE /ex/template/{templateId}).")]
        public async Task<TextContent> DeleteUserTemplate(string username, string password, string templateId, DeleteTemplateRequest request)
        {
            await DoxiAPIWrapper.DeleteUserTemplate(username, password, templateId, request);
            return Success(new { templateId });
        }

        [McpServerTool(Name = "GetTemplate"), Description("Retrieves template metadata (GET /ex/template/{templateId}).")]
        public async Task<TextContent> GetTemplate(string username, string password, string templateId)
        {
            var result = await DoxiAPIWrapper.GetTemplate(username, password, templateId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "DeleteAttachmentFromTemplate"), Description("Deletes an attachment from a template (DELETE /ex/template/{templateId}/attachments/{attachmentId}).")]
        public async Task<TextContent> DeleteAttachmentFromTemplate(string username, string password, string templateId, string attachmentId)
        {
            await DoxiAPIWrapper.DeleteAttachmentFromTemplate(username, password, templateId, attachmentId);
            return Success(new { templateId, attachmentId });
        }

        [McpServerTool(Name = "DocumentInfo"), Description("Extracts information from a document file (POST /ex/document/info).")]
        public async Task<TextContent> DocumentInfo(string username, string password, byte[] document)
        {
            var result = await DoxiAPIWrapper.DocumentInfo(username, password, document);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "DocumentInfoBase64"), Description("Extracts information from a document provided as base64 (POST /ex/document/info/base64).")]
        public async Task<TextContent> DocumentInfoBase64(string username, string password, GetDocumentInfoRquest request)
        {
            var result = await DoxiAPIWrapper.DocumentInfoBase64(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SearchInDocumentBase64"), Description("Searches for text or patterns in a base64-encoded document (POST /ex/document/search/base64).")]
        public async Task<TextContent> SearchInDocumentBase64(string username, string password, SearchInDocumentBase64Request request)
        {
            var result = await DoxiAPIWrapper.SearchInDocumentBase64(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "SearchInDocument"), Description("Searches for text or patterns in an uploaded document (POST /ex/document/search).")]
        public async Task<TextContent> SearchInDocument(string username, string password, byte[] file, SearchInDocumentRequest request)
        {
            var result = await DoxiAPIWrapper.SearchInDocument(username, password, file, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "MergeDocuments"), Description("Merges multiple documents into a single file (POST /ex/document/merge).")]
        public async Task<DataContent> MergeDocuments(string username, string password, IEnumerable<byte[]> documents)
        {
            var bytes = await DoxiAPIWrapper.MergeDocuments(username, password, documents);
            return new DataContent(bytes, "application/pdf");
        }

        // =======================
        // KITS
        // =======================

        [McpServerTool(Name = "AddKit"), Description("Creates a new kit (POST /ex/kit).")]
        public async Task<TextContent> AddKit(string username, string password, ExAddKitRequest request)
        {
            var result = await DoxiAPIWrapper.AddKit(username, password, request);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "UpdateKit"), Description("Updates an existing kit (PUT /ex/kit/{kitId}).")]
        public async Task<TextContent> UpdateKit(string username, string password, string kitId, ExUpdateKitRequest request)
        {
            await DoxiAPIWrapper.UpdateKit(username, password, kitId, request);
            return Success(new { kitId });
        }

        [McpServerTool(Name = "GetKit"), Description("Retrieves kit metadata (GET /ex/kit/{kitId}).")]
        public async Task<TextContent> GetKit(string username, string password, string kitId)
        {
            var result = await DoxiAPIWrapper.GetKit(username, password, kitId);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetKits"), Description("Lists kits for the tenant (GET /ex/kit).")]
        public async Task<TextContent> GetKits(string username, string password)
        {
            var result = await DoxiAPIWrapper.GetKits(username, password);
            return new TextContent(ToJson(result));
        }

        // =======================
        // USER OPERATIONS
        // =======================

        [McpServerTool(Name = "GetUserGroups"), Description("Retrieves groups for a user identified by key type and value (GET /ex/user/{searchType}/{searchValue}/groups).")]
        public async Task<TextContent> GetUserGroups(string username, string password, ParticipantKeyType searchType, string searchValue)
        {
            var result = await DoxiAPIWrapper.GetUserGroups(username, password, searchType, searchValue);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetUserTemplates"), Description("Retrieves templates of a user identified by key type and value (GET /ex/user/{searchType}/{searchValue}/templates).")]
        public async Task<TextContent> GetUserTemplates(string username, string password, ParticipantKeyType searchType, string searchValue)
        {
            var result = await DoxiAPIWrapper.GetUserTemplates(username, password, searchType, searchValue);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetUserIdByEmail"), Description("Retrieves the user ID by email (GET /ex/user/byEmail/{email}/id).")]
        public async Task<TextContent> GetUserIdByEmail(string username, string password, string email)
        {
            var result = await DoxiAPIWrapper.GetUserIdByEmail(username, password, email);
            return new TextContent(ToJson(result));
        }

        [McpServerTool(Name = "GetUsers"), Description("Queries users with filter parameters (GET /ex/users).")]
        public async Task<TextContent> GetUsers(string username, string password, Dictionary<string, object> queryParams)
        {
            var result = await DoxiAPIWrapper.GetUsers(username, password, queryParams);
            return new TextContent(ToJson(result));
        }

        /// <summary>
        /// Create Doxi template from PDF file, templateFileBase64 parameter is the pdf file needed to create the template. The templateInstructions parameter needed for telling Doxi all the information on creating the template
        /// </summary>
        /// <param name="username">api username</param>
        /// <param name="password">api password</param>
        /// <param name="templateInstructions">instruction on the template needs to be created</param>
        /// <param name="inputFileBase64">The template document (PDF/Word/Image) in base64</param>
        /// <returns></returns>
        [McpServerTool(Name = "AddTemplate"),
            Description("Create a Doxi template. The 'inputFileBase64' parameter MUST receive the base64 file uploaded by the user (PDF/DOCX/Image).Parameter prompt contains all other prompt text that the client add.")]
        public async Task<TextContent> AddTemplate(string username, string password,string prompt, string inputFileBase64)
        {
            var templateFile = Convert.FromBase64String(inputFileBase64);
            var result = await TemplateLogic.AddTemplate(username, password, templateFile, prompt);
            return new TextContent(ToJson(result));
        }
    }
}
