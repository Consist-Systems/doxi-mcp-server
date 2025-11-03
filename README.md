Doxi Sign MCP Server (.NET Core)
=================================

Overview
--------
This project provides a Model Context Protocol (MCP) Server implemented in ASP.NET Core for managing document signing flows via the Doxi Sign API.

It exposes JSON-RPC 2.0 methods that allow AI clients (ChatGPT, Claude, etc.) to create, edit, and manage signing flows, documents, and attachments.

------------------------------------------------------------------

Setup Guide
-----------

1. Clone & build
   git clone https://github.com/yourorg/doxi-mcp-server.git
   cd doxi-mcp-server
   dotnet build

2. Run locally
   dotnet run

   Default endpoint:
   http://localhost:5287/{Your Company Name}/mcp

------------------------------------------------------------------

Headers
-------
All requests must include:
   Mcp-Session-Id: test
   Content-Type: application/json

------------------------------------------------------------------

Example Requests
----------------

List available tools:
----------------------
POST http://localhost:5287/{Your Company Name}/mcp
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list"
}

Example response:
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": { "tools": [...] }
}

------------------------------------------------------------------

Get Flow Status Example:
------------------------
POST http://localhost:5287/{Your Company Name}/mcp
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "GetFlowStatus",
    "arguments": {
      "username": "apiuser",
      "password": "apipass",
      "signFlowId": "123456"
    }
  }
}

Response:
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": { "status": "Approved" }
}

------------------------------------------------------------------

Available Tools
---------------
1. **GetAllWebhookSubscription** — Lists all webhook subscriptions (GET /ex/webhook).
2. **DeleteUserTemplate** — Deletes a user template (DELETE /ex/template/{templateId}).
3. **GetFormSettings** — Downloads form settings for a company form (GET /ex/company/{companyId}/forms/{formId}/settings).
4. **ReplaceSigner** — Replaces an existing signer in a signing flow (POST /flow/ReplaceSigner).
5. **MergeDocuments** — Merges multiple documents into a single file (POST /ex/document/merge).
6. **DocumentInfoBase64** — Extracts information from a document provided as base64 (POST /ex/document/info/base64).
7. **AddTemplate** — Creates a new user template (POST /ex/template).
8. **GetFlowAttachments** — Gets all attachments of a flow (GET /flow/{signFlowId}/attachments).
9. **AddAttachmentAsBase64ToFlow** — Adds a base64-encoded attachment to a flow (POST /flow/{signFlowId}/attachments/base64).
10. **DeleteSubscription** — Deletes a webhook subscription (DELETE /ex/webhook/{subscriptionId}).
11. **GetFlowAttachmentField** — Gets a specific attachment field from a flow (POST /flow/{signFlowId}/AttachmentField).
12. **UpdateKit** — Updates an existing kit (PUT /ex/kit/{kitId}).
13. **EditSignFlow** — Edits an existing signing flow (POST /flow/edit).
14. **SetSignatures** — Updates or adds signatures to a flow (POST /flow/{signFlowId}/SetSignatures).
15. **GetKits** — Lists kits for the tenant (GET /ex/kit).
16. **SearchWebhookCallLogs** — Searches webhook call logs for a subscription (POST /ex/webhook/{subscriptionId}/logs/search).
17. **GetUserTemplates** — Retrieves templates of a user identified by key type and value (GET /ex/user/{searchType}/{searchValue}/templates).
18. **GetFlowsStatus** — Retrieves statuses for multiple flows at once (POST /flow/status).
19. **WebHookCheck** — Validates and previews a webhook payload (POST /ex/webhook/check).
20. **UpdateWebhookSubscription** — Updates a webhook subscription (PUT /ex/webhook/{subscriptionId}).
21. **GetFlowStatus** — Gets the current status of a specific flow (GET /flow/{signFlowId}/status).
22. **UpdateTemplate** — Updates an existing template (PUT /ex/template/{templateId}).
23. **GetTemplate** — Retrieves template metadata (GET /ex/template/{templateId}).
24. **DeleteAttachmentFromTemplate** — Deletes an attachment from a template (DELETE /ex/template/{templateId}/attachments/{attachmentId}).
25. **GetUserIdByEmail** — Retrieves the user ID by email (GET /ex/user/byEmail/{email}/id).
26. **GetUsers** — Queries users with filter parameters (GET /ex/users).
27. **GetFlow** — Retrieves metadata for a specific signing flow (GET /flow/{signFlowId}).
28. **SearchInDocumentBase64** — Searches for text or patterns in a base64-encoded document (POST /ex/document/search/base64).
29. **GetAllFlows** — Retrieves all signing flows available for the current tenant (GET /flow).
30. **DocumentInfo** — Extracts information from a document file (POST /ex/document/info).
31. **SearchInDocument** — Searches for text or patterns in an uploaded document (POST /ex/document/search).
32. **CreateFlowFromTemplate** — Creates a new signing flow from a template (POST /ex/template/CreateFlowFromTemplate/{templateId}).
33. **GetUserGroups** — Retrieves groups for a user identified by key type and value (GET /ex/user/{searchType}/{searchValue}/groups).
34. **SearchFlow** — Searches signing flows by filters such as signer, date, or status (POST /flow/search).
35. **AddKit** — Creates a new kit (POST /ex/kit).
36. **AddSubscription** — Adds a new webhook subscription (POST /ex/webhook).
37. **GetKit** — Retrieves kit metadata (GET /ex/kit/{kitId}).
38. **GetDocument** — Downloads a document from a signing flow (GET /flow/{signFlowId}/Document).
39. **AddSignFlow** — Creates a new signing flow and uploads a document (POST /flow).
40. **SetFlowAction** — Sets an action on a flow such as approve, reject, or delegate (POST /flow/{signFlowId}/action).
------------------------------------------------------------------

Integration (Claude / ChatGPT)
------------------------------

Windows config (claude_desktop_config.json):
{
  "mcpServers": {
    "doxi_flows": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\path\to\DoxiMcpServer.csproj"
      ]
    }
  }
}

macOS/Linux:
{
  "mcpServers": {
    "doxi_flows": {
      "command": "/usr/local/bin/dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/DoxiMcpServer.csproj"
      ]
    }
  }
}

------------------------------------------------------------------

Troubleshooting
---------------
- Bad Request: Mcp-Session-Id header is required → Add header.
- Session not found → Use fixed session ID.
- Method not available → Ensure method name is tools/list or tools/call.
- Empty tool list → Verify metadata.json is loaded.

------------------------------------------------------------------

License
-------
Licensed under the MIT License.
