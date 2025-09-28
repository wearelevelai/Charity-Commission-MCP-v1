# CCEW MCP Constitution

## Purpose & Scope

The *CCEW MCP* is an open-source Model Context Protocol (MCP) server that surfaces Charity Commission for England & Wales guidance published on GOV.UK via the Content API.  
- **Scope**: Read-only retrieval of Charity Commission guidance hosted on GOV.UK (`/api/content/{path}` and `/api/search.json`).  
- **Out of scope**: Legal advice, private/third-party sources, or any alteration of guidance beyond non-lossy enrichment.  
- **License**: Server released under MIT; upstream GOV.UK content licensed under [Open Government Licence v3.0](https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/).  

All responses MUST include attribution (“Source: GOV.UK, Charity Commission guidance, OGL v3.0”) and a disclaimer (“This is guidance, not legal advice.”).

## Core Principles

### I. Protocol-First Interoperability (MCP)

The server MUST implement the Model Context Protocol (MCP) for all tools/resources
exposed to agents. All capabilities MUST be modeled as MCP tools/resources with
declared input/output schemas. Non-standard or hidden endpoints are prohibited.
Compatibility tests MUST verify behavior across standard MCP clients.

### II. Source Fidelity & Read‑Only Safety

The system MUST retrieve Charity Commission guidance via the GOV.UK Content API
and operate strictly read‑only against upstreams. Responses MUST preserve
canonical source, include the GOV.UK URL and content identifiers, and clearly
label any non‑lossy enrichment. Caching MUST use explicit TTLs and honor
cache‑busting rules; stale data MUST NOT be served beyond policy.

### III. Contracts and Tests First (NON‑NEGOTIABLE)

Contract and integration tests MUST be written before implementation. Public MCP
tool/resource schemas MUST be versioned and validated via automated tests.
CI MUST execute contract, integration, and unit tests on every PR; merges are
blocked on failing tests or Constitution gates.

### IV. Observability, Tracing, and Audit

Structured logging MUST include correlation IDs, upstream latency, cache
hit/miss, request/response sizes (redacted), and MCP tool invocations. Tracing
MUST propagate across agent calls when hosted via Azure AI Foundry. Logs MUST
exclude PII and avoid full payloads by default (allow capped 4KB previews under
debug flags). Target SLOs: cached p95 < 300ms; live fetch p95 < 1000ms.

### V. Versioning, Compatibility, and Simplicity

Semantic Versioning (MAJOR.MINOR.PATCH) applies to all public MCP tools and
schemas. Breaking changes REQUIRE a MAJOR bump and a deprecation window ≥ 30
days with migration notes. Additive changes MUST be backward compatible. Prefer
the simplest design meeting requirements; remove unused complexity.


### VI. Ethical Guardrails

- All outputs MUST include attribution and disclaimer.  
- The MCP server MUST never present outputs as legal advice.  
- Misleading or hallucinated responses are prohibited.

## Tool Inventory

The MCP server MUST expose the following tools/resources for interoperability:

- **get_content_by_path**  
  Input: `{ path: string }`  
  Output: `{ content: object, url: string, public_updated_at: string, attribution: string }`  
  Retrieves authoritative GOV.UK content for a given path.

  Example Input:
  {
    "path": "/government/publications/charity-trustee-duties"
  }

  Example Output:
  {
    "content": { ... },
    "url": "https://www.gov.uk/government/publications/charity-trustee-duties",
    "public_updated_at": "2025-09-01T12:34:56Z",
    "attribution": "Source: GOV.UK, Charity Commission guidance, OGL v3.0"
  }

  JSON Schema (Input):
  {
    "type": "object",
    "properties": {
      "path": { "type": "string", "description": "The GOV.UK content path (e.g. /government/publications/...)" }
    },
    "required": ["path"]
  }

  JSON Schema (Output):
  {
    "type": "object",
    "properties": {
      "content": { "type": "object" },
      "url": { "type": "string", "format": "uri" },
      "public_updated_at": { "type": "string", "format": "date-time" },
      "attribution": { "type": "string" }
    },
    "required": ["content", "url", "public_updated_at", "attribution"]
  }

- **search_guidance**  
  Input: `{ query: string, filters?: object }`  
  Output: `{ results: array of { title, url, summary, public_updated_at } }`  
  Wraps the GOV.UK `/api/search.json` endpoint, constrained to Charity Commission content.

  Example Input:
  {
    "query": "annual return",
    "filters": { "organisation": "charity-commission" }
  }

  Example Output:
  {
    "results": [
      {
        "title": "Charity annual returns",
        "url": "https://www.gov.uk/guidance/charity-annual-returns",
        "summary": "How to complete your charity’s annual return.",
        "public_updated_at": "2025-07-14T09:00:00Z"
      }
    ]
  }

  JSON Schema (Input):
  {
    "type": "object",
    "properties": {
      "query": { "type": "string" },
      "filters": { "type": "object", "additionalProperties": true }
    },
    "required": ["query"]
  }

  JSON Schema (Output):
  {
    "type": "object",
    "properties": {
      "results": {
        "type": "array",
        "items": {
          "type": "object",
          "properties": {
            "title": { "type": "string" },
            "url": { "type": "string", "format": "uri" },
            "summary": { "type": "string" },
            "public_updated_at": { "type": "string", "format": "date-time" }
          },
          "required": ["title", "url", "public_updated_at"]
        }
      }
    },
    "required": ["results"]
  }

- **get_source_metadata**  
  Input: `{ path: string }`  
  Output: `{ url: string, identifiers: object, last_updated: string, licence: string }`  
  Returns provenance metadata and identifiers for the requested resource.

  Example Input:
  {
    "path": "/government/publications/charity-annual-return"
  }

  Example Output:
  {
    "url": "https://www.gov.uk/government/publications/charity-annual-return",
    "identifiers": { "content_id": "abc-123-xyz" },
    "last_updated": "2025-08-21T15:22:10Z",
    "licence": "OGL v3.0"
  }

  JSON Schema (Input):
  {
    "type": "object",
    "properties": {
      "path": { "type": "string" }
    },
    "required": ["path"]
  }

  JSON Schema (Output):
  {
    "type": "object",
    "properties": {
      "url": { "type": "string", "format": "uri" },
      "identifiers": { "type": "object" },
      "last_updated": { "type": "string", "format": "date-time" },
      "licence": { "type": "string" }
    },
    "required": ["url", "last_updated", "licence"]
  }

- **force_refresh**  
  Input: `{ path: string }`  
  Output: `{ refreshed: boolean, timestamp: string }`  
  Forces cache bypass and fetches the latest available version from GOV.UK.

  Example Input:
  {
    "path": "/government/publications/charity-annual-return"
  }

  Example Output:
  {
    "refreshed": true,
    "timestamp": "2025-09-28T10:00:00Z"
  }

  JSON Schema (Input):
  {
    "type": "object",
    "properties": {
      "path": { "type": "string" }
    },
    "required": ["path"]
  }

  JSON Schema (Output):
  {
    "type": "object",
    "properties": {
      "refreshed": { "type": "boolean" },
      "timestamp": { "type": "string", "format": "date-time" }
    },
    "required": ["refreshed", "timestamp"]
  }

- **get_error_taxonomy**  
  Output: `{ errors: array of { code, description } }`  
  Exposes the standardised error codes supported by the MCP server.

  Example Output:
  {
    "errors": [
      { "code": "UPSTREAM_RATE_LIMITED", "description": "GOV.UK API rate limit exceeded" },
      { "code": "NOT_FOUND_OR_REDIRECTED", "description": "Requested path not found or redirected" },
      { "code": "STALE_CACHE_SERVED", "description": "Stale cache served due to upstream failure" },
      { "code": "CONTENT_OUT_OF_SCOPE", "description": "Content is not part of Charity Commission guidance" }
    ]
  }

  JSON Schema (Output):
  {
    "type": "object",
    "properties": {
      "errors": {
        "type": "array",
        "items": {
          "type": "object",
          "properties": {
            "code": { "type": "string" },
            "description": { "type": "string" }
          },
          "required": ["code", "description"]
        }
      }
    },
    "required": ["errors"]
  }

## Additional Constraints & Security

- Technology stack: .NET 8 LTS for the server; standard MCP tooling for
  interoperability; Azure AI Foundry as an agent host/consumer.  
- Upstream integration: Respect GOV.UK Content API terms, rate limits, and
  caching headers. Apply exponential backoff and circuit‑breaker patterns.  
- Secrets and configuration: No secrets in source control. Use environment
  variables / secret stores. All outgoing endpoints must be configurable.  
- Data handling: No user PII is stored. Logs MUST not contain sensitive data.
  Content payloads in logs MUST be redacted or size‑capped.  
- Caching: Explicit TTLs, cache key strategy includes source identifiers and
  query params. Provide cache‑bypass for forced refresh.  
- Reliability targets: p95 latency and uptime SLOs defined in service SLO doc;
  alerts on SLO breaches; graceful degradation to live fetch when cache fails.

- Upstream access: implement `/api/content/{path}` and `/api/search.json` as the primary GOV.UK integrations; attachments/images may be referenced but not served directly.  
- Rate limiting: enforce ≤10 requests per second per client; apply jittered exponential backoff on upstream contention; surface `UPSTREAM_RATE_LIMITED` errors when relevant.  
- Error taxonomy: standardise error codes including `UPSTREAM_RATE_LIMITED`, `NOT_FOUND_OR_REDIRECTED`, `STALE_CACHE_SERVED`, and `CONTENT_OUT_OF_SCOPE`.  
- Dependency hygiene: Dependencies MUST be pinned; publish a Software Bill of Materials (SBOM) on each release; remediate vulnerabilities per SLA (Critical ≤7 days, High ≤14 days).  
- Incident response: Maintain documented playbooks; incidents (security breach, SLO failure) trigger rollback, notification, and post-mortem within 14 days.  

## Development Workflow & Quality Gates

- TDD is mandatory: write failing contract/integration tests first.  
- Constitution Check gate MUST pass in plans and during PR review.  
- Code review by designated owners; changes to contracts require reviewer with
  protocol ownership.  
- CI gates: build, tests, lint, schema validation, and license checks.  
- Commits follow conventional commits; docs accompany behavioral changes.  
- Release process enforces SemVer and publishes migration notes for changes.

## Governance

This Constitution supersedes conflicting practices. Amendments MUST be proposed
via PR including: change rationale, impact analysis, migration plan (if user‑
visible), and updated tests/gates where applicable. Approval requires consent
from protocol owner(s) and one additional maintainer. Versioning policy:

- MAJOR: Backward‑incompatible governance or principle redefinitions.  
- MINOR: New principle/section added or materially expanded guidance.  
- PATCH: Clarifications, wording, or non‑semantic refinements.  


Compliance reviews occur at least quarterly and on release candidates. Violations
MUST be remediated or explicitly documented in Complexity Tracking with a
time‑boxed mitigation plan.



**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): set by project owner on adoption | **Last Amended**: 2025-09-28
