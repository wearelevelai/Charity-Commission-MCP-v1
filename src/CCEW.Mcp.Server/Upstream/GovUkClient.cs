using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CCEW.Mcp.Server.Upstream;

public interface IGovUkClient
{
    Task<GovUkSearchResponse> SearchAsync(GovUkSearchRequest req, CancellationToken ct);
    Task<GovUkContentItem?> GetContentByPathAsync(string path, CancellationToken ct);
    Task<GovUkContentItem?> GetContentByIdAsync(string contentId, CancellationToken ct);
}

public class GovUkClient(HttpClient http, ILogger<GovUkClient> logger) : IGovUkClient
{
    private readonly HttpClient _http = http;
    private readonly ILogger<GovUkClient> _logger = logger;

    public async Task<GovUkSearchResponse> SearchAsync(GovUkSearchRequest req, CancellationToken ct)
    {
        // Map to GOV.UK search API
        // Docs: https://www.gov.uk/api/search.json
        var query = new Dictionary<string, string?>
        {
            ["q"] = req.Query,
            ["start"] = ((req.Page - 1) * req.PageSize).ToString(),
            ["count"] = req.PageSize.ToString(),
            ["filter_organisations"] = req.Organisation,
            ["filter_format"] = req.Format,
            ["order"] = "-public_timestamp"
        };
        if (req.PublicTimestampFrom is not null)
            query["filter_public_timestamp"] = $">={req.PublicTimestampFrom:O}";
        if (req.PublicTimestampTo is not null)
            query["filter_public_timestamp"] = (query["filter_public_timestamp"] is { Length: > 0 } f)
                ? $"{f},<={req.PublicTimestampTo:O}"
                : $"<={req.PublicTimestampTo:O}";

        // Build query string without additional deps
        var encoded = new FormUrlEncodedContent(query.Where(kv => kv.Value is not null)!
            .Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))!);
        var qs = await encoded.ReadAsStringAsync(ct);
        var uri = $"/api/search.json?{qs}";
        using var rsp = await _http.GetAsync(uri, ct);
        if (rsp.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            // GOV.UK returns 422 for invalid filters
            throw new GovUkParameterException("Invalid parameters for search");
        }
        rsp.EnsureSuccessStatusCode();
        var json = await rsp.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var total = doc.RootElement.TryGetProperty("total", out var totalEl) && totalEl.TryGetInt32(out var t) ? t : 0;
        var results = new List<GovUkSearchResultItem>();
        if (doc.RootElement.TryGetProperty("results", out var resEl) && resEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in resEl.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? string.Empty : string.Empty;
                var webUrl = item.TryGetProperty("link", out var lEl) ? $"https://www.gov.uk{lEl.GetString()}" : null;
                var summary = item.TryGetProperty("description", out var dEl) ? dEl.GetString() : null;
                var updated = item.TryGetProperty("public_timestamp", out var uEl) ? uEl.GetString() : null;
                var contentId = item.TryGetProperty("content_id", out var cEl) ? cEl.GetString() : null;
                if (webUrl is null || string.IsNullOrEmpty(title))
                    continue;
                results.Add(new GovUkSearchResultItem(title, webUrl, summary, updated, contentId));
            }
        }
        return new GovUkSearchResponse(results, total);
    }

    public async Task<GovUkContentItem?> GetContentByPathAsync(string path, CancellationToken ct)
    {
        // Content API: https://www.gov.uk/api/content/{path}
        var normalized = path.StartsWith('/') ? path : "/" + path;
        using var rsp = await _http.GetAsync($"/api/content{normalized}", ct);
        if (rsp.StatusCode == HttpStatusCode.NotFound)
            return null;
        rsp.EnsureSuccessStatusCode();
        var json = await rsp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone();
        var contentId = root.TryGetProperty("content_id", out var idEl) ? idEl.GetString() : null;
        var publicUpdatedAt = root.TryGetProperty("public_updated_at", out var puaEl) ? puaEl.GetString() : null;
        var canonicalPath = root.TryGetProperty("base_path", out var bpEl) ? bpEl.GetString() : normalized;
        var url = canonicalPath is null ? null : $"https://www.gov.uk{canonicalPath}";
        return new GovUkContentItem(url, publicUpdatedAt, contentId, root);
    }

    public async Task<GovUkContentItem?> GetContentByIdAsync(string contentId, CancellationToken ct)
    {
        // Attempt to resolve content by ID using search, then fetch by path
        var query = new Dictionary<string, string?>
        {
            ["filter_content_id"] = contentId,
            ["count"] = "1"
        };
        var encoded = new FormUrlEncodedContent(query.Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))!);
        var qs = await encoded.ReadAsStringAsync(ct);
        var uri = $"/api/search.json?{qs}";
        using var rsp = await _http.GetAsync(uri, ct);
        if (rsp.StatusCode == HttpStatusCode.NotFound)
            return null;
        rsp.EnsureSuccessStatusCode();
        var json = await rsp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var resEl) || resEl.ValueKind != JsonValueKind.Array)
            return null;
        var first = resEl.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
            return null;
        var link = first.TryGetProperty("link", out var lEl) ? lEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(link))
            return null;
        return await GetContentByPathAsync(link!, ct);
    }
}

public record GovUkSearchRequest(
    string Query,
    int Page,
    int PageSize,
    string? Organisation,
    string? Format,
    DateTimeOffset? PublicTimestampFrom,
    DateTimeOffset? PublicTimestampTo
);

public record GovUkSearchResponse(IReadOnlyList<GovUkSearchResultItem> Results, int Total);
public record GovUkSearchResultItem(string Title, string Url, string? Summary, string? PublicUpdatedAt, string? ContentId);

public record GovUkContentItem(string? Url, string? PublicUpdatedAt, string? ContentId, JsonElement Raw);

public class GovUkParameterException(string message) : Exception(message);
