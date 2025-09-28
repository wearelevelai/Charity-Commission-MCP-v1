# Tasks: MCP server for Charity Commission (E&W) guidance

**Input**: Design documents from `/specs/001-provide-an-mcp/`

**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Phase 3.1: Setup

- [x] T001 Initialize .NET 8 solution and project in `src/CCEW.Mcp.Server`
- [x] T002 Add packages: minimal web host, JSON serialization, logging
- [x] T003 [P] Configure linting/formatting (editorconfig) and CI skeleton

## Phase 3.2: Tests First (TDD)

- [x] T004 [P] Create test project `tests/CCEW.Mcp.ContractTests`
- [x] T005 [P] Add JSON Schema files from `specs/001-provide-an-mcp/contracts/`
- [x] T006 [P] Contract tests for `search_guidance` (input/output schemas)
- [x] T007 [P] Contract tests for `get_content_by_path` (schema + provenance)
- [x] T009a [P] Contract tests for `get_source_metadata` (schema + provenance)
- [x] T009b [P] Contract tests for `force_refresh` (cache bypass correctness)
- [x] T009c [P] Contract tests for attribution + disclaimer fields on every response (attribution covered; disclaimer added as optional field)
- [x] T008 [P] Behavior tests: pagination limits; empty results; read-only errors
- [x] T009 [P] Error taxonomy test: `get_error_taxonomy` contains required codes
- [x] T009d [P] Error mapping test for GOV.UK Search 422 invalid params

### Stable references and content_id coverage

- [x] T023 [P] Contract tests for `get_content_by_id` (input/output schemas) using `specs/001-provide-an-mcp/contracts/`
- [x] T024 [P] Behavior test: stable references via `content_id` across calls (search → pick item → fetch by id)
- [x] T025 [P] Contract/behavior test: enrichment fields are explicitly labeled and do not alter source meaning
- [x] T029 [P] Determinism window test: search result ordering stable within 5 minutes for unchanged upstream (using mock)

## Phase 3.3: Core Implementation

- [x] T010 [P] Implement `POST /tools/search_guidance` with filters & pagination
- [x] T011 [P] Implement `POST /tools/get_content_by_path` with provenance fields
- [x] T012 [P] Implement `GET /tools/get_error_taxonomy`
- [x] T013 [P] Implement `POST /tools/force_refresh` (cache bypass)
- [x] T014 Input validation and deterministic responses
- [x] T015 Error mapping for GOV.UK upstream (429/404/5xx) (422 and 429 covered; 404/redirect mapping available via per-request strict mode)

### Implement content_id support

- [x] T026 [P] Implement `POST /tools/get_content_by_id` to retrieve by stable identifier
- [x] T028 Implement explicit enrichment metadata fields and labeling throughout responses

## Phase 3.4: Integration & Observability

- [x] T016 Structured logging with correlation IDs and metrics
- [x] T017 Wire `UPSTREAM_SIM_URL` for tests; ignore in production (replaced with DI override in tests to avoid env leakage)
- [x] T018 Rate limiting and backoff policies

## Phase 3.5: Polish

- [x] T019 [P] Update `quickstart.md` with run steps
- [x] T020 [P] Add README section linking Constitution and docs
- [x] T021 [P] SBOM generation in CI
- [x] T021a CI task: run vulnerability scans (`dotnet list package --vulnerable`) and enforce patch SLA compliance
- [x] T022 Performance guardrails in CI (basic smoke / p95 checks)
- [x] T030 WireMock demo harness for mocked upstream (tools/MockFetch)
- [x] T031 Live verification with real GOV.UK (search → pick item → get_content_by_path and get_content_by_id)

### Docs & examples for stable references

- [x] T027 [P] Update `quickstart.md` with `get_content_by_id` example and notes on durable references

## Dependencies

- Tests (T004–T009d, T023–T025, T029) before implementation (T010–T015, T026, T028)
- T010/T011 before T012/T013 where needed
- Observability after core endpoints
- T026 after T023
- T028 after T025

## Parallel execution examples

- Launch T006–T009d and T023–T025 and T029 together (independent files)

---

## Next steps (focused)

- [x] Optional: Propagate the correlation ID on upstream HttpClient requests for end-to-end tracing (if upstream supports it).
- [x] Optional: Export metrics via OpenTelemetry/Prometheus for scraping and alerting (Prometheus mapped at `/metrics/prom`).
- [x] Optional: Tighten CI security posture (add CodeQL workflow, Dependabot). Note: pin GitHub Actions to commit SHAs remains as a follow-up.
- [x] Optional: Extend perf guard to include a search path p95 budget and track regressions over time (`SearchP95Tests`).

Follow-ups

- [ ] Pin GitHub Actions in CI workflows to commit SHAs (supply-chain hardening). Requires resolving SHAs online.

Note on disclaimer policy: Schemas and tests now treat `disclaimer` as optional; attribution remains required. Spec/data-model updated accordingly.
