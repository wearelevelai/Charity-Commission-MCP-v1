# Research: MCP server for Charity Commission (E&W) guidance

 
## Decisions

- Pagination: Offset-based pagination using GOV.UK `start` + `count`; default 20 per page, max 100. Future option: expose cursor abstraction if stability needed.
- Filters: Limit to organisation=charity-commission, format, and public_timestamp range to keep scope clear and relevant.
- Response fields: Include title, summary/description, url, public_updated_at, content_id; include section headings/attachments when present.
- Error taxonomy: Standardised codes (UPSTREAM_RATE_LIMITED, NOT_FOUND_OR_REDIRECTED, STALE_CACHE_SERVED, CONTENT_OUT_OF_SCOPE).
- Read-only: No mutations; provide force_refresh tool to bypass cache when explicitly requested.

 
## Rationale

- Agent ergonomics: Deterministic schemas and pagination defaults simplify planning and tool-use for AI clients.
- Source fidelity: Provenance is mandatory for trust (GOV.UK URL + content_id).
- Operability: Error taxonomy enables robust recovery and user messaging.
- Scope control: Filters align the search to Charity Commission guidance and reduce noise.
- Performance: Pagination default (20) balances payload size with usability; 100 cap avoids excessive payloads.

 
## Alternatives Considered

- Free-form filters/advanced search: Rejected for initial scope; increases complexity and ambiguity for agents.
- Offset-based pagination: Using GOV.UK's native `start` + `count`. Cursor-based pagination considered but rejected for MVP; may be revisited for stability under dataset changes.

 
## Future Work

- Attachments representation: Provide structured metadata only; do not proxy files. Confirm approach in future iteration.
- Section extraction: Derive headings from GOV.UK JSON when available; otherwise omit. Confirm parsing strategy in later release.
