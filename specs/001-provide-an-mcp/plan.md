# Implementation Plan: MCP server for Charity Commission (E&W) guidance

**Branch**: `001-provide-an-mcp` | **Date**: 2025-09-28 | **Spec**: `/Users/sim0nall3n/Documents/GitHub/CCEW_MCP/specs/001-provide-an-mcp/spec.md`
**Input**: Feature specification from `/specs/001-provide-an-mcp/spec.md`

## Execution Flow (/plan command scope)

```text
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Provide an MCP server that exposes read-only Charity Commission (England & Wales)
guidance hosted on GOV.UK via first-class MCP tools, enabling AI agents to
search and retrieve guidance with strong provenance. Technical approach: .NET 8
MCP server exposing tools `search_guidance`, `get_content_by_path`,
`get_source_metadata`, `force_refresh`, and `get_error_taxonomy`. Read-only,
provenance-preserving integration with GOV.UK Content API; deterministic request
/ response schemas for agent reasoning; pagination and error taxonomy for robust
automated handling.

 
## Technical Context
**Language/Version**: .NET 8 LTS  
**Primary Dependencies**: MCP server framework/tooling (.NET), HttpClient for GOV.UK API, JSON processing  
**Storage**: N/A (read-only; optional in-memory/ephemeral cache)  
**Testing**: .NET xUnit; schema validation (JsonSchema.Net); WireMock.Net for upstream error simulation  
**Target Platform**: Linux server (containerized), Azure-hosted agents consume via MCP  
**Project Type**: single  
**Performance Goals**: cached p95 < 300ms; live fetch p95 < 1000ms  
**Constraints**: Strict read-only; provenance required; explicit TTLs; rate limiting; deterministic schemas; Pagination defaults clarified (20 per page, max 100)  
**Scale/Scope**: External consumption by AI agents; result pagination (default 20, max 100)

 
## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*


Initial Constitution Check: PASS
Checked against Constitution v1.0.0 (2025-09-28): PASS

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
├── unit/
   └── CCEW.Mcp.ContractTests/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: Single project. Adopt `src/` and `tests/` at repo root
for .NET solution; add `tests/CCEW.Mcp.ContractTests` per contract scaffold.

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with clarifications resolved

## Phase 1: Design & Contracts
Prerequisite: research.md complete

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)
   - Validate requests/responses using JSON Schemas in `specs/001-provide-an-mcp/contracts/`

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/bash/update-agent-context.sh copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, quickstart.md, agent-specific file (tests defined in scaffold for follow-up)

## Phase 2: Task Planning Approach
Note: This section describes what the /tasks command will do - do not execute during /plan.

**Task Generation Strategy**:

**Ordering Strategy**:

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
Note: The following phases are beyond the scope of the /plan command.

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
Note: Fill this section only if the Constitution Check has violations that must be justified.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
Note: This checklist is updated during execution flow.

**Phase Status**: Phase 0 research complete; Phase 1 design/contracts complete.  
**Gate Status**: Constitution Check PASS (initial and post-design)

*Based on Constitution v1.0.0 - See `.specify/memory/constitution.md`*
