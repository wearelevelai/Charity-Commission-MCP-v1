# Data Model: MCP server for Charity Commission (E&W) guidance

## Entities

### GuidanceItem

- title: string  
- summary: string  
- url: uri (canonical GOV.UK web URL)  
- content_id: string (stable identifier from GOV.UK Content API)  
- public_updated_at: datetime (ISO‑8601)  
- provenance: object { api_path: string, licence: string = "OGL v3.0" }  
- attribution: string (e.g., "Source: GOV.UK, Charity Commission guidance, OGL v3.0")  
- disclaimer: string (optional; e.g., "This is guidance, not legal advice.")  
- stale: boolean (optional; true if served from cache after upstream failure)  
- sections: array of { heading: string, anchor?: string, level?: integer } (optional)  
- attachments: array of { title: string, url: uri, content_type?: string, file_extension?: string } (optional)

### SearchQuery

- query: string  
- filters: object (organisation, format, public_timestamp range: { from?: datetime, to?: datetime })  
- page: integer (default 1)  
- pageSize: integer (default 20, max 100)  
- note: server maps page/pageSize → GOV.UK offset params (`start` = (page‑1)*pageSize, `count` = pageSize)

### ResultSet

- items: array of GuidanceItem  
- total: integer (optional; as provided by GOV.UK Search API)  
- page: integer  
- pageSize: integer  
- hasNext: boolean  
- hasPrev: boolean  
- message: string (optional)

## Validation Rules

- url MUST start with <https://www.gov.uk/>
- public_updated_at MUST be ISO-8601
- pageSize MUST be between 1 and 100
- organisation filter, when specified, MUST equal "charity-commission"

- attribution MUST be present on every GuidanceItem (string, non-empty)
- disclaimer SHOULD be present on content retrieval responses where applicable; it is optional in the contract and may be omitted when a client provides its own legal notice.
- when filters.organisation is present, its value MUST equal "charity-commission"
- when filters.public_timestamp is present, any provided `from`/`to` MUST be ISO‑8601 datetimes
