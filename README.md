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
**Flows**
- **AddAttachmentAsBase64ToFlow** — Adds a base64-encoded attachment to a signing flow. `POST /flow/{signFlowId}/attachments/base64`
- **AddSignFlow** — Creates a new signing flow and uploads a document for signing. `POST /flow`
- **EditSignFlow** — Edits an existing signing flow, replacing documents or parameters. `POST /flow/edit`
- **GetAllFlows** — Retrieves all signing flows available for the current tenant. `GET /flow`
- **GetDocument** — Downloads a document (PDF) associated with a signing flow. `GET /flow/{signFlowId}/Document/{withSigns}`
- **GetFlow** — Retrieves metadata for a specific signing flow. `GET /flow/{signFlowId}`
- **GetFlowAttachmentField** — Retrieves a specific attachment field from a flow. `POST /flow/{signFlowId}/AttachmentField`
- **GetFlowAttachments** — Retrieves all attachments associated with a flow. `GET /flow/{signFlowId}/attachments`
- **GetFlowsStatus** — Retrieves statuses for multiple flows. `POST /flow/status`
- **GetFlowStatus** — Retrieves current status for a single flow. `GET /flow/{signFlowId}/status`
- **ReplaceSigner** — Replaces a signer in an existing flow. `POST /flow/ReplaceSigner`
- **SearchFlow** — Searches flows using filters (status, date, signer, etc.). `POST /flow/search`
- **SetFlowAction** — Performs an action on a flow (approve, reject, etc.). `POST /flow/{signFlowId}/action`
- **SetSignatures** — Sets or updates signatures for a specific flow. `POST /flow/{signFlowId}/SetSignatures`

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
