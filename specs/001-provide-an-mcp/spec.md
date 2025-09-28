# Feature Specification: MCP server for Charity Commission (E&W) guidance

**Feature Branch**: `001-provide-an-mcp`  
**Created**: 2025-09-28  
**Status**: Draft  
**Input**: User description: "Provide an MCP server that exposes read-only Charity Commission (England & Wales) guidance hosted on GOV.UK, via first-class MCP tools that are easy for AI agents to call and reason over. Why MCP. MCP is an open standard that lets AI clients (Claude, ChatGPT, Azure agents, etc.) connect to external data/tools through a consistent interface. Think \"USB-C for AI\"."

## Execution Flow (main)

```text
1. Parse user description from Input
   → If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   → Identify: actors, actions, data, constraints
3. For each unclear aspect:
   → Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   → If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   → Each requirement must be testable
   → Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   → If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   → If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## ⚡ Quick Guidelines

- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

### Section Requirements

- **Mandatory sections**: Must be completed for every feature
- **Optional sections**: Include only when relevant to the feature
- When a section doesn't apply, remove it entirely (don't leave as "N/A")

### For AI Generation

When creating this spec from a user prompt:

1. **Mark all ambiguities**: Use [NEEDS CLARIFICATION: specific question] for any assumption you'd need to make
2. **Don't guess**: If the prompt doesn't specify something (e.g., "login system" without auth method), mark it
3. **Think like a tester**: Every vague requirement should fail the "testable and unambiguous" checklist item
4. **Common underspecified areas**:
   - User types and permissions
   - Data retention/deletion policies  
   - Performance targets and scale
   - Error handling behaviors
   - Integration requirements
   - Security/compliance needs

---

## User Scenarios & Testing *(mandatory)*

### Primary User Story

As an AI agent connected via MCP, I need to search and fetch Charity Commission
(England & Wales) guidance from GOV.UK through a consistent, well-documented MCP
tool interface so that I can answer user questions with source-linked, reliable,
read-only information.

### Acceptance Scenarios

1. Given an AI agent is connected to the MCP server, when it calls
    `search_guidance` with query "trustee conflicts of interest", then it
    receives a list of relevant guidance items including title, short summary,
    GOV.UK URL, and a stable source identifier.
2. Given the agent has a specific GOV.UK URL or content identifier, when it
    calls `get_content_by_path` with that identifier, then it receives the guidance
    metadata and structured content fields (e.g., title, summary/description,
    section headings if available) plus provenance (URL, identifier).
3. Given the server operates read-only, when any mutating operation is
    attempted, then the server returns a standardized read-only error and no
    changes occur.
4. Given a query with no matching guidance, when the agent calls
    `search_guidance`, then it receives an empty result set with an explanatory
    message and no error.

### Edge Cases

- Upstream API unavailable or times out → return a structured error indicating
   upstream unavailability without exposing sensitive details.
- Upstream rate-limits the request → return a structured rate limit error with
   backoff advice suitable for agent planning.
- Ambiguous query that matches many items → return results with clear ordering
   and provide optional disambiguation guidance in a message field.
- Very large content → return content in a way that enables agent consumption
   without truncating meaning; include a mechanism to page or segment long
   results. Pagination/segmentation: Use offset-based pagination (GOV.UK `start` + `count`) for search results (default 20 per page, max 100) and segment long guidance items by section headings when available.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose first-class MCP tools to search and retrieve
   Charity Commission (England & Wales) guidance hosted on GOV.UK.
- **FR-002**: System MUST operate strictly read-only; any attempted mutation MUST
   return a standardized read-only error with no side effects.
- **FR-003**: Every item returned MUST include provenance: the canonical GOV.UK
   URL and a stable source identifier.
- **FR-003a**: All responses MUST include attribution ("Source: GOV.UK, Charity Commission guidance, OGL v3.0"). Responses SHOULD include a disclaimer (e.g., "This is guidance, not legal advice.") where applicable. Disclaimer is optional in the contracts and may be omitted by clients that present their own legal notice.
- **FR-004**: The system MUST support keyword search over guidance content and/or
   metadata, returning results ordered for relevance.
- **FR-005**: The system MUST support retrieval of a specific guidance item by
   GOV.UK URL and by stable source identifier.
- **FR-006**: Search results and items MUST include fields that enable agent
   reasoning (e.g., title, short summary/description, key headings when
   available) and clear links back to the source.
- **FR-007**: Any enrichment or transformation included in responses MUST be
   clearly labeled as such and MUST NOT alter the original meaning of the source
   content.
- **FR-008**: Tools MUST define deterministic request/response schemas that are
   consistent across calls, enabling agents to plan and reason reliably.
- **FR-009**: The system MUST return an empty result (not an error) when no
   guidance matches a valid search query, with an optional explanatory message.
- **FR-010**: Upstream failures (e.g., unavailability, rate limiting) MUST be
   mapped to structured error outputs suitable for agent handling, without
   exposing sensitive internal details.
- **FR-011**: The system MUST support pagination for search results using offset-based parameters (GOV.UK `start` + `count`), with a default page size of 20 and a maximum of 100 per page.
- **FR-012**: The system MUST provide a stable mechanism to reference items in
   follow-up calls (e.g., by URL or identifier) to support multi-step agent
   workflows.

### Clarifications

- **FR-A01**: Allowed search filters include `organisation` (restricted to `charity-commission`), `format`, and `public_timestamp` range.
- **FR-A02**: Response fields MUST include: `title`, `summary/description`, `url`, `public_updated_at`, `content_id`, and optional section headings/attachments when present in GOV.UK JSON.
- **FR-A03**: Pagination defaults as above.
- **FR-A04**: Pagination uses offset-based parameters (`start` + `count`) and MUST be deterministic for the same query within a short time window (allowing for upstream content changes).

### Key Entities *(include if feature involves data)*

- **GuidanceItem**: A single GOV.UK Charity Commission guidance artifact. Key
   attributes: title, summary, canonical URL, content_id (stable identifier), last updated date, optional section titles, attachments metadata.
- **SearchQuery**: User-provided query text and optional filters/facets.
   Attributes: free-text query, optional filters (organisation, format, public_timestamp range), page (default 1), page size (default 20, max 100).
- **ResultSet**: A paginated set of `GuidanceItem` results. Attributes: items,
   total (if available), page indicators, optional disambiguation guidance.

---

## Review & Acceptance Checklist

GATE: Automated checks run during main() execution

### Content Quality

- [ ] No implementation details (languages, frameworks, APIs)
- [ ] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [ ] All mandatory sections completed

### Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [ ] Requirements are testable and unambiguous  
- [ ] Success criteria are measurable
- [ ] Scope is clearly bounded
- [ ] Dependencies and assumptions identified

---

## Execution Status

Updated by main() during processing

- [ ] User description parsed
- [ ] Key concepts extracted
- [ ] Ambiguities marked
- [ ] User scenarios defined
- [ ] Requirements generated
- [ ] Entities identified
- [ ] Review checklist passed

---

## Contract Test Scaffold (.NET xUnit)

> This scaffold provides a concrete, runnable outline for contract tests that enforce the Specification and Constitution. It assumes a .NET 8 toolchain and focuses on validating request/response schemas, error mappings, pagination, and performance guards. It **does not** mock GOV.UK itself for normal runs; instead it uses WireMock.Net to simulate upstream edge/error cases.

### 1) Project layout

```
/tests
  /CCEW.Mcp.ContractTests
    CCEW.Mcp.ContractTests.csproj
    /Schemas
      get_content_by_path.input.schema.json
      get_content_by_path.output.schema.json
      search_guidance.input.schema.json
      search_guidance.output.schema.json
      get_source_metadata.input.schema.json
      get_source_metadata.output.schema.json
      force_refresh.input.schema.json
      force_refresh.output.schema.json
      get_error_taxonomy.output.schema.json
    /Fixtures
      stable_paths.json                // known-stable GOV.UK paths for smoke tests
    Contract/                          // schema compliance, output fields, determinism
    Behavior/                          // pagination, filters, errors, force refresh
    Performance/                       // simple p95 guard-rails (stopwatch-based)
```

### 2) csproj (packages)

Use:
- `xunit` and `xunit.runner.visualstudio`
- `FluentAssertions` for expressive assertions
- `JsonSchema.Net` (and `JsonSchema.Net.Data` if you split schemas) for JSON Schema validation
- `WireMock.Net` to simulate upstream 422/404/429/5xx cases
- `System.Text.Json` for canonical JSON handling

Example `CCEW.Mcp.ContractTests.csproj` excerpt:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.7.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="JsonSchema.Net" Version="5.*" />
    <PackageReference Include="WireMock.Net" Version="1.6.*" />
  </ItemGroup>
</Project>
```

> Versions are indicative; pin to the latest stable minor versions you approve during setup.

### 3) Test configuration

Set the MCP server base URL via environment variable:

- `MCP_BASE_URL` — e.g., `http://localhost:5057` during local runs.
- `UPSTREAM_SIM_URL` — the WireMock.Net server URL used only for error/edge-case simulations.

### 4) Minimal test helpers

```csharp
// tests/CCEW.Mcp.ContractTests/Contract/SchemaValidator.cs
using System.Text.Json;
using Json.Schema;

public static class SchemaValidator
{
    public static void Validate(string json, string schemaJson)
    {
        var schema = JsonSchema.FromText(schemaJson);
        var doc = JsonDocument.Parse(json);
        var result = schema.Evaluate(doc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!result.IsValid)
        {
            var errors = string.Join("\n - ", result.Details.Select(d => d.Message));
            throw new Xunit.Sdk.XunitException($"Schema validation failed:\n - {errors}");
        }
    }
}
```

### 5) Example contract tests

#### 5.1 `get_content_by_path` — happy path

```csharp
// tests/CCEW.Mcp.ContractTests/Contract/GetContentByPathContractTests.cs
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

public class GetContentByPathContractTests
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("MCP_BASE_URL")!) };

    [Fact]
    public async Task ReturnsRequiredFields_AndMatchesSchema()
    {
        var payload = new { path = "/guidance/charity-annual-returns" };
        using var resp = await _http.PostAsJsonAsync("/tools/get_content_by_path", payload);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        // Load schema strings (e.g., from embedded resources or Schemas folder)
        var outputSchema = await File.ReadAllTextAsync("Schemas/get_content_by_path.output.schema.json");
        SchemaValidator.Validate(json, outputSchema);

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("url").GetString().Should().StartWith("https://www.gov.uk/");
        root.GetProperty("public_updated_at").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("attribution").GetString().Should().Contain("GOV.UK");
    }
}
```

#### 5.2 `search_guidance` — pagination and filters

```csharp
// tests/CCEW.Mcp.ContractTests/Behavior/SearchGuidanceBehaviorTests.cs
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

public class SearchGuidanceBehaviorTests
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("MCP_BASE_URL")!) };

    [Fact]
    public async Task Paginates_AndHonoursPageSizeLimits()
    {
        var page1 = await _http.PostAsJsonAsync("/tools/search_guidance", new { query = "annual return", filters = new { organisation = "charity-commission" }, page = 1, pageSize = 20 });
        var page2 = await _http.PostAsJsonAsync("/tools/search_guidance", new { query = "annual return", filters = new { organisation = "charity-commission" }, page = 2, pageSize = 20 });

        page1.EnsureSuccessStatusCode();
        page2.EnsureSuccessStatusCode();

        var j1 = await page1.Content.ReadAsStringAsync();
        var j2 = await page2.Content.ReadAsStringAsync();

        j1.Should().NotBe(j2); // naive guard that different pages differ
    }

    [Fact]
    public async Task Rejects_OversizedPageSize()
    {
        var resp = await _http.PostAsJsonAsync("/tools/search_guidance", new { query = "annual return", page = 1, pageSize = 9999 });
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
```

#### 5.3 Error taxonomy surfaced

```csharp
// tests/CCEW.Mcp.ContractTests/Contract/ErrorTaxonomyContractTests.cs
using FluentAssertions;
using Xunit;

public class ErrorTaxonomyContractTests
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("MCP_BASE_URL")!) };

    [Fact]
    public async Task ContainsStandardCodes()
    {
        var json = await _http.GetStringAsync("/tools/get_error_taxonomy");
        json.Should().Contain("UPSTREAM_RATE_LIMITED")
            .And.Contain("NOT_FOUND_OR_REDIRECTED")
            .And.Contain("STALE_CACHE_SERVED")
            .And.Contain("CONTENT_OUT_OF_SCOPE");
    }
}
```

#### 5.4 Force refresh bypasses cache

```csharp
// tests/CCEW.Mcp.ContractTests/Behavior/ForceRefreshBehaviorTests.cs
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

public class ForceRefreshBehaviorTests
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("MCP_BASE_URL")!) };

    [Fact]
    public async Task BypassesCacheAndReturnsTimestamp()
    {
        var path = "/guidance/charity-annual-returns";
        // warm cache
        await _http.PostAsJsonAsync("/tools/get_content_by_path", new { path });

        var refresh = await _http.PostAsJsonAsync("/tools/force_refresh", new { path });
        refresh.EnsureSuccessStatusCode();

        var json = await refresh.Content.ReadAsStringAsync();
        json.Should().Contain("\"refreshed\":true").And.Contain("timestamp");
    }
}
```

#### 5.5 Upstream error mapping via WireMock.Net

```csharp
// tests/CCEW.Mcp.ContractTests/Behavior/UpstreamErrorMappingTests.cs
using FluentAssertions;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

public class UpstreamErrorMappingTests : IDisposable
{
    private readonly WireMockServer _mock = WireMockServer.Start();
    private readonly HttpClient _http = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("MCP_BASE_URL")!) };

    public UpstreamErrorMappingTests()
    {
        Environment.SetEnvironmentVariable("UPSTREAM_SIM_URL", _mock.Urls[0]);

        // 422 invalid parameter simulation for search
        _mock.Given(Request.Create().WithPath("/api/search.json").UsingGet())
             .RespondWith(Response.Create().WithStatusCode(422).WithBody("{\"error\":\"invalid parameter\"}"));

        // 429 rate limit simulation for content
        _mock.Given(Request.Create().WithPath("/api/content/*").UsingGet())
             .RespondWith(Response.Create().WithStatusCode(429));
    }

    [Fact]
    public async Task Maps422ToClientError()
    {
        var resp = await _http.PostAsync("/tools/search_guidance", new StringContent("{\"query\":\"x\",\"filters\":{\"bad\":\"param\"}}", System.Text.Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("UPSTREAM_PARAMETER_ERROR")
            .Or.Contain("client parameter error"); // depending on your final mapping
    }

    [Fact]
    public async Task Maps429ToUpstreamRateLimited()
    {
        var resp = await _http.PostAsync("/tools/get_content_by_path", new StringContent("{\"path\":\"/guidance/anything\"}", System.Text.Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("UPSTREAM_RATE_LIMITED");
    }

    public void Dispose() => _mock.Stop();
}
```

> Note: You may expose a configuration toggle so the MCP server uses `UPSTREAM_SIM_URL` for tests; in production it should ignore this setting.

### 6) Fixtures

Provide a curated list of stable GOV.UK paths in `Fixtures/stable_paths.json` (examples are illustrative — verify during setup):

```json
{
  "paths": [
    "/guidance/charity-annual-returns",
    "/guidance/charity-trustee-roles-and-responsibilities"
  ]
}
```

### 7) Running the suite

```
# from repository root, with MCP server running locally
export MCP_BASE_URL=http://localhost:5057
dotnet test tests/CCEW.Mcp.ContractTests
```

### 8) CI wiring

- Add a `contract-test` job to your CI that:
  - Builds the MCP server and starts it locally (test profile).
  - Runs the WireMock.Net container (or in-process server).
  - Executes `dotnet test` and publishes results.
- Fail the build on any schema violations or missing required fields.

---

This scaffold should be sufficient for contributors to go from zero to green tests without guesswork. Keep the schemas in sync with the Constitution; the suite is your enforcement layer.
