# CCEW MCP Server

[![CI](https://github.com/sim0nall3n/CCEW_MCP/actions/workflows/ci.yml/badge.svg)](https://github.com/sim0nall3n/CCEW_MCP/actions/workflows/ci.yml)

Model Context Protocol (MCP) server exposing Charity Commission (E&W) guidance from GOV.UK with deterministic contracts and clear provenance.

## Key features

- .NET 8 minimal API with tests-first contracts
- Tools:
  - `search_guidance` → GOV.UK search mapping with pagination and stable ordering
  - `get_content_by_path` → Fetch content with provenance and optional enrichment
  - `get_content_by_id` → Resolve by stable `content_id` via GOV.UK search
  - `get_source_metadata`, `get_error_taxonomy`, `force_refresh`
- Optional enrichment block (`options.include_enrichment`) clearly labeled
- Optional strict upstream errors (`options.strict_upstream_errors`)
- Resiliency via Polly; structured logging (Serilog)

## Quickstart

See `specs/001-provide-an-mcp/quickstart.md` for run instructions and request/response examples.

## Try it live

- Search guidance
  - POST /tools/search_guidance
  - Example body: { "query": "charity", "page": 1, "pageSize": 5, "filters": { "organisation": "charity-commission" } }

- Fetch by path
  - POST /tools/get_content_by_path
  - Example body: { "path": "/guidance/get-help-for-your-inactive-or-ineffective-charity" }
  - See trimmed example in `specs/001-provide-an-mcp/examples/get_content_by_path.json`.

- Fetch by content_id
  - POST /tools/get_content_by_id
  - Example body: { "content_id": "2cd95b28-c7fd-415a-b710-f85386feb4df" }
  - See trimmed example in `specs/001-provide-an-mcp/examples/get_content_by_id.json`.

## Development

- Build & test locally:

```bash
 dotnet test --nologo --verbosity:minimal
```

- CI runs on pushes/PRs to main (see badge above).

### Run locally

Minimal run:

```bash
dotnet run --project src/CCEW.Mcp.Server
```

Environment variables (optional tuning):

- RATE_LIMIT_PER_WINDOW (default: 60)
- RATE_LIMIT_WINDOW_SECONDS (default: 60)
- CACHE_TTL_SEARCH_SECONDS (default: 30)
- CACHE_TTL_CONTENT_SECONDS (default: 60)

Health: <http://localhost:5240/healthz> (actual port may vary unless you set ASPNETCORE_URLS)

### Container

Build and run the container:

```bash
docker build -t ccew-mcp:local .
docker run -p 8080:8080 \
  -e RATE_LIMIT_PER_WINDOW=60 \
  -e RATE_LIMIT_WINDOW_SECONDS=60 \
  -e CACHE_TTL_SEARCH_SECONDS=30 \
  -e CACHE_TTL_CONTENT_SECONDS=60 \
  ccew-mcp:local
```

Health: <http://localhost:8080/healthz>

### CI security and compliance

The GitHub Actions workflow includes:

- NuGet vulnerability scanning using `dotnet list package --vulnerable --include-transitive`.
  - The build fails if vulnerable packages are detected; the full report is uploaded as an artifact (`dotnet-audit`).
- SBOM generation in CycloneDX JSON format using `anchore/sbom-action`.
  - The SBOM (`sbom/bom.json`) is uploaded as a build artifact.

## Docs

- Constitution, plan, specs in `specs/001-provide-an-mcp/`
- Contracts under `specs/001-provide-an-mcp/contracts/`

## License

- Source attribution: GOV.UK, Charity Commission guidance, OGL v3.0.
