# Quickstart: CCEW MCP Server

This guide shows how to run the MCP server locally and exercise its tools as an AI agent would.

## Prerequisites

- .NET 8 SDK
- macOS/Linux shell
- Docker (optional)

## Run

You can run the server locally either via .NET or Docker:

```bash
dotnet run --project src/CCEW.Mcp.Server
# or
docker compose up mcp-server
```

Verify the health endpoint responds:

```bash
curl http://localhost:8080/healthz
```

## Call tools (HTTP shim for local testing)

Until an MCP client is configured, you can exercise the tool endpoints directly:

- `POST /tools/search_guidance` with JSON body:

```json
{
  "query": "annual return",
  "filters": { "organisation": "charity-commission" },
  "page": 1,
  "pageSize": 20
}
```

- `POST /tools/get_content_by_path` with JSON body:

```json
{ "path": "/guidance/charity-annual-returns" }
```

- `POST /tools/get_content_by_id` with JSON body:

```json
{ "content_id": "00000000-0000-0000-0000-000000000000" }
```

### Optional enrichment

You can request an additional, clearly-separated enrichment block on content responses. This block is intended for derived annotations produced by the server (not source content) and is always labeled with `is_enrichment: true`.

- To include enrichment, add `options.include_enrichment = true` to the request body:

```json
{
  "path": "/guidance/charity-annual-returns",
  "options": { "include_enrichment": true }
}
```

Or by content id:

```json
{
  "content_id": "00000000-0000-0000-0000-000000000000",
  "options": { "include_enrichment": true }
}
```

Expected shape in responses (truncated):

```json
{
  "content": { /* source JSON from GOV.UK */ },
  "url": "https://www.gov.uk/...",
  "public_updated_at": "2025-09-01T12:34:56Z",
  "attribution": "Source: GOV.UK, Charity Commission guidance, OGL v3.0",
  "content_id": "...", // present on id/path when available
  "enrichment": {
    "is_enrichment": true,
    "notes": "free-form annotations and derived fields may appear here"
  }
}
```

### Strict upstream errors (optional)

By default, when upstream returns 404/redirects for a content path or ID, the server responds with a safe fallback envelope (empty content with provenance and attribution). To opt into strict error mapping (useful for tests or callers who prefer hard failures), set `options.strict_upstream_errors = true` in the request body for `get_content_by_path` or `get_content_by_id`.

Example:

```json
{
  "path": "/missing/path",
  "options": { "strict_upstream_errors": true }
}
```

When strict mode is enabled and content is not found, the server returns:

```json
{
  "code": "NOT_FOUND_OR_REDIRECTED",
  "error": "Requested path not found or redirected"
}
```

- `POST /tools/get_source_metadata` with JSON body:

```json
{}
```

- `POST /tools/force_refresh` with JSON body:

```json
{ "url": "https://www.gov.uk/guidance/charity-annual-returns" }
```

- `POST /tools/get_error_taxonomy` with empty JSON body:

```json
{ }
```

Expected behavior:

- Read-only responses with provenance (GOV.UK URL + identifiers)
- Deterministic schemas matching `/specs/001-provide-an-mcp/contracts/*`
- Clear error mapping for upstream issues (rate limiting, not found)
- Durable references: use `content_id` for follow-up calls when available (see `get_content_by_id`)

Example response (truncated):
{
  "url": "<https://www.gov.uk/guidance/charity-annual-returns>",
  "public_updated_at": "2025-09-01T12:34:56Z",
  "attribution": "Source: GOV.UK, Charity Commission guidance, OGL v3.0",
  "disclaimer": "This is guidance, not legal advice."
}


Example error response:
{
  "error": {
    "code": "NOT_FOUND_OR_REDIRECTED",
    "message": "Requested path not found or redirected"
  }
}

## Troubleshooting

- **422 Unprocessable Entity from GOV.UK Search API**  
  Cause: Invalid or unsupported search parameter.  
  Fix: Check that filters only include `organisation`, `format`, or `public_timestamp` range. Remove unknown keys.

- **429 Too Many Requests**  
  Cause: Rate limit exceeded against GOV.UK API.  
  Fix: Reduce request frequency. The server applies jittered exponential backoff automatically; retry after delay.

- **404 / 410 Not Found / Redirected**  
  Cause: The requested GOV.UK path no longer exists or has moved.  
  Fix: Use the canonical GOV.UK URL in the error response; update the client to point to the new location.

- **Stale cache served**  
  Cause: GOV.UK API temporarily unavailable, cached data returned.  
  Fix: Retry later or use `force_refresh` once upstream recovers.

- **Server not starting**  
  Cause: Missing .NET 8 runtime or misconfigured Docker.  
  Fix: Verify .NET 8 SDK installation (`dotnet --info`) or ensure Docker service is running.

## Next steps

- Implement endpoints and contracts per `data-model.md` and `contracts/`.
- Wire up contract tests (`tests/CCEW.Mcp.ContractTests/`) and validate against schemas in `specs/001-provide-an-mcp/contracts/`.
