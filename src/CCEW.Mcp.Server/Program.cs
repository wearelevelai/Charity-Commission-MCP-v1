using System.Text.Json;
using System.Text.Json.Serialization;
using CCEW.Mcp.Server.Upstream;
using System.Collections.Concurrent;
using System.Diagnostics;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Context;
using Prometheus;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Serilog basic console logging
Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.CreateLogger();
builder.Host.UseSerilog();

// Configure JSON options for deterministic serialization
builder.Services.ConfigureHttpJsonOptions(o =>
{
	o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
	o.SerializerOptions.WriteIndented = false;
});

// Access HttpContext for outbound header propagation
builder.Services.AddHttpContextAccessor();

// Upstream GOV.UK HttpClient with resiliency (register BEFORE Build)
builder.Services.AddHttpClient<IGovUkClient, GovUkClient>(client =>
{
	// Always point to GOV.UK in the app; tests can override via DI in the factory
	client.BaseAddress = new Uri("https://www.gov.uk");
	client.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler(sp => new CorrelationPropagationHandler(sp.GetRequiredService<IHttpContextAccessor>()))
.AddPolicyHandler(HttpPolicyExtensions
	.HandleTransientHttpError()
	.OrResult(r => (int)r.StatusCode == 429)
	.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt))));

var app = builder.Build();

// Correlation + Metrics middleware
app.Use(async (context, next) =>
{
	// Correlation ID: take from header or generate
	var corrHeaderName = "X-Correlation-ID";
	var correlationId = context.Request.Headers.TryGetValue(corrHeaderName, out var hv) && !string.IsNullOrWhiteSpace(hv.ToString())
		? hv.ToString()
		: Guid.NewGuid().ToString("N");
	context.Response.Headers[corrHeaderName] = correlationId;

	// Metrics: count + duration + status breakdown
	Interlocked.Increment(ref AppMetrics.TotalRequests);
	AppMetrics.RequestsByPath.AddOrUpdate(context.Request.Path.HasValue ? context.Request.Path.Value! : "/",
		1, (_, c) => c + 1);

	using (LogContext.PushProperty("CorrelationId", correlationId))
	{
		var sw = Stopwatch.StartNew();
		try
		{
			await next();
		}
		finally
		{
			sw.Stop();
			Interlocked.Add(ref AppMetrics.TotalRequestMs, sw.ElapsedMilliseconds);
			var status = context.Response?.StatusCode ?? 0;
			AppMetrics.StatusCounts.AddOrUpdate(status, 1, (_, c) => c + 1);
			if (status >= 400)
			{
				Interlocked.Increment(ref AppMetrics.TotalErrors);
			}
		}
	}
});
// Expose Prometheus metrics for scraping (in addition to JSON metrics)
app.UseHttpMetrics();
app.MapMetrics("/metrics/prom");

// Health (explicit serialize to avoid PipeWriter issues under mixed runtimes)
app.MapGet("/healthz", () =>
{
	var json = JsonSerializer.Serialize(new { status = "ok" }, new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	});
	return Results.Text(json, "application/json");
});

// Basic metrics endpoint (JSON)
app.MapGet("/metrics", () =>
{
	var total = Interlocked.Read(ref AppMetrics.TotalRequests);
	var errors = Interlocked.Read(ref AppMetrics.TotalErrors);
	var totalMs = Interlocked.Read(ref AppMetrics.TotalRequestMs);
	var avgMs = total > 0 ? (double)totalMs / total : 0d;
	var payload = new
	{
		total_requests = total,
		total_errors = errors,
		avg_ms = Math.Round(avgMs, 2),
		status_counts = AppMetrics.StatusCounts.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
		path_counts = AppMetrics.RequestsByPath.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value)
	};
	var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	});
	return Results.Text(json, "application/json");
});

// Tool endpoints (stubs with gradual upstream wiring)
app.MapPost("/tools/search_guidance", async (JsonElement body, IGovUkClient govuk, CancellationToken ct) =>
{
	var query = body.TryGetProperty("query", out var qEl) ? qEl.GetString() : null;
	if (string.IsNullOrWhiteSpace(query))
	{
		var err = JsonSerializer.Serialize(new { error = "query is required" }, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false
		});
		return Results.Text(err, "application/json", statusCode: 400);
	}
	int page = body.TryGetProperty("page", out var pEl) && pEl.TryGetInt32(out var p) ? Math.Max(1, p) : 1;
	int pageSize = body.TryGetProperty("pageSize", out var sEl) && sEl.TryGetInt32(out var s) ? Math.Clamp(s, 1, 100) : 20;

	string? organisation = null;
	string? format = null;
	DateTimeOffset? from = null;
	DateTimeOffset? to = null;
	if (body.TryGetProperty("filters", out var fEl) && fEl.ValueKind == JsonValueKind.Object)
	{
		if (fEl.TryGetProperty("organisation", out var orgEl) && orgEl.ValueKind == JsonValueKind.String)
		{
			organisation = orgEl.GetString();
		}
		if (fEl.TryGetProperty("format", out var fmtEl) && fmtEl.ValueKind == JsonValueKind.String)
		{
			format = fmtEl.GetString();
		}
		if (fEl.TryGetProperty("public_timestamp_from", out var fromEl))
		{
			if (fromEl.ValueKind == JsonValueKind.String && !DateTimeOffset.TryParse(fromEl.GetString(), out var fromParsed))
			{
				var err = JsonSerializer.Serialize(new { code = "UPSTREAM_PARAMETER_ERROR", error = "Invalid public_timestamp_from" }, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
					WriteIndented = false
				});
				return Results.Text(err, "application/json", statusCode: 400);
			}
			else if (fromEl.ValueKind == JsonValueKind.String)
			{
				DateTimeOffset.TryParse(fromEl.GetString(), out var fromOk);
				from = fromOk;
			}
		}
		if (fEl.TryGetProperty("public_timestamp_to", out var toEl))
		{
			if (toEl.ValueKind == JsonValueKind.String && !DateTimeOffset.TryParse(toEl.GetString(), out var toParsed))
			{
				var err = JsonSerializer.Serialize(new { code = "UPSTREAM_PARAMETER_ERROR", error = "Invalid public_timestamp_to" }, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
					WriteIndented = false
				});
				return Results.Text(err, "application/json", statusCode: 400);
			}
			else if (toEl.ValueKind == JsonValueKind.String)
			{
				DateTimeOffset.TryParse(toEl.GetString(), out var toOk);
				to = toOk;
			}
		}
	}

	try
	{
		var req = new GovUkSearchRequest(
			Query: query!,
			Page: page,
			PageSize: pageSize,
			Organisation: organisation,
			Format: format,
			PublicTimestampFrom: from,
			PublicTimestampTo: to
		);
		var upstream = await govuk.SearchAsync(req, ct);

		// Stable ordering by public_timestamp descending
		var ordered = upstream.Results
			.OrderByDescending(r => DateTimeOffset.TryParse(r.PublicUpdatedAt, out var dt) ? dt : DateTimeOffset.MinValue)
			.ToList();

		var mapped = ordered.Select(r => new
		{
			title = r.Title,
			url = r.Url,
			summary = r.Summary,
			public_updated_at = r.PublicUpdatedAt,
			content_id = r.ContentId
		}).ToArray();

		var payload = new { results = mapped, page, pageSize, total = upstream.Total };
		var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false
		});
		return Results.Text(json, "application/json");
	}
	catch (GovUkParameterException ex)
	{
		var err = JsonSerializer.Serialize(new { code = "UPSTREAM_PARAMETER_ERROR", error = ex.Message }, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false
		});
		return Results.Text(err, "application/json", statusCode: 400);
	}
	catch (HttpRequestException ex)
	{
		var err = JsonSerializer.Serialize(new { code = "UPSTREAM_RATE_LIMITED", error = ex.Message }, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false
		});
		return Results.Text(err, "application/json", statusCode: 503);
	}
});

app.MapPost("/tools/get_content_by_path", async (JsonElement body, IGovUkClient govuk, CancellationToken ct) =>
{
	var includeEnrichment = body.TryGetProperty("options", out var optEl)
		&& optEl.ValueKind == JsonValueKind.Object
		&& optEl.TryGetProperty("include_enrichment", out var incEl)
		&& incEl.ValueKind == JsonValueKind.True;

	object? enrichment = includeEnrichment
		? new { is_enrichment = true, notes = "stubbed enrichment" }
		: null;

	var path = body.TryGetProperty("path", out var pEl) ? pEl.GetString() : null;
	GovUkContentItem? upstream = null;
	if (!string.IsNullOrWhiteSpace(path))
	{
		try
		{
			upstream = await govuk.GetContentByPathAsync(path!, ct);
		}
		catch (HttpRequestException ex)
		{
			var err = JsonSerializer.Serialize(new { code = "UPSTREAM_RATE_LIMITED", error = ex.Message });
			return Results.Text(err, "application/json", statusCode: 503);
		}
	}
	var strictErrors = body.TryGetProperty("options", out var optsEl)
		&& optsEl.ValueKind == JsonValueKind.Object
		&& optsEl.TryGetProperty("strict_upstream_errors", out var strictEl)
		&& strictEl.ValueKind == JsonValueKind.True;
	if (strictErrors && upstream is null)
	{
		var err = JsonSerializer.Serialize(new { code = "NOT_FOUND_OR_REDIRECTED", error = "Requested path not found or redirected" }, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false
		});
		return Results.Text(err, "application/json", statusCode: 404);
	}
	var payload = new
	{
		content = upstream?.Raw.ValueKind == JsonValueKind.Object ? (object)upstream.Raw : new { },
		url = upstream?.Url ?? "https://www.gov.uk/",
		public_updated_at = upstream?.PublicUpdatedAt ?? DateTime.UtcNow.ToString("o"),
		attribution = "Source: GOV.UK, Charity Commission guidance, OGL v3.0",
		disclaimer = "This guidance is not legal advice.",
		content_id = upstream?.ContentId ?? Guid.Empty.ToString(),
		enrichment
	};
	var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	});
	return Results.Text(json, "application/json");
});

app.MapPost("/tools/get_content_by_id", async (JsonElement body, IGovUkClient govuk, CancellationToken ct) =>
{
	var requestedId = body.TryGetProperty("content_id", out var idEl) ? idEl.GetString() : null;

	var includeEnrichment = body.TryGetProperty("options", out var optEl)
		&& optEl.ValueKind == JsonValueKind.Object
		&& optEl.TryGetProperty("include_enrichment", out var incEl)
		&& incEl.ValueKind == JsonValueKind.True;

	var strictErrors = body.TryGetProperty("options", out var optsEl)
		&& optsEl.ValueKind == JsonValueKind.Object
		&& optsEl.TryGetProperty("strict_upstream_errors", out var strictEl)
		&& strictEl.ValueKind == JsonValueKind.True;

	GovUkContentItem? upstream = null;
	if (!string.IsNullOrWhiteSpace(requestedId))
	{
		try
		{
			upstream = await govuk.GetContentByIdAsync(requestedId!, ct);
		}
		catch (HttpRequestException ex)
		{
			var err = JsonSerializer.Serialize(new { code = "UPSTREAM_RATE_LIMITED", error = ex.Message }, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				WriteIndented = false
			});
			return Results.Text(err, "application/json", statusCode: 503);
		}
	}

	if (strictErrors && !string.IsNullOrWhiteSpace(requestedId) && upstream is null)
	{
		var err = JsonSerializer.Serialize(new { code = "NOT_FOUND_OR_REDIRECTED", error = "Requested content not found or redirected" }, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false
		});
		return Results.Text(err, "application/json", statusCode: 404);
	}

	object? enrichment = includeEnrichment
		? new { is_enrichment = true, notes = "stubbed enrichment" }
		: null;

	var payload = new
	{
		content = upstream?.Raw.ValueKind == JsonValueKind.Object ? (object)upstream.Raw : new { },
		url = upstream?.Url ?? "https://www.gov.uk/",
		public_updated_at = upstream?.PublicUpdatedAt ?? DateTime.UtcNow.ToString("o"),
		attribution = "Source: GOV.UK, Charity Commission guidance, OGL v3.0",
		disclaimer = "This guidance is not legal advice.",
		content_id = upstream?.ContentId ?? (requestedId ?? Guid.Empty.ToString()),
		enrichment
	};
	var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	});
	return Results.Text(json, "application/json");
});

app.MapGet("/tools/get_source_metadata", () =>
{
	var payload = new
	{
		organisation = "charity-commission",
		source = "GOV.UK Content API",
		base_url = "https://www.gov.uk/api",
		documentation_url = "https://www.gov.uk/api"
	};
	var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	});
	return Results.Text(json, "application/json");
});

app.MapGet("/tools/get_error_taxonomy", () =>
{
	var payload = new
	{
		errors = new[]
		{
			new { code = "UPSTREAM_RATE_LIMITED", description = "GOV.UK API rate limit exceeded" },
			new { code = "NOT_FOUND_OR_REDIRECTED", description = "Requested path not found or redirected" },
			new { code = "STALE_CACHE_SERVED", description = "Stale cache served due to upstream failure" },
			new { code = "CONTENT_OUT_OF_SCOPE", description = "Content is not part of Charity Commission guidance" },
			new { code = "UPSTREAM_PARAMETER_ERROR", description = "Invalid parameters supplied to upstream GOV.UK API" }
		}
	};
	// Workaround for test host PipeWriter issue under mixed runtimes: serialize explicitly to string
	var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	});
	return Results.Text(json, "application/json");
});

app.MapPost("/tools/force_refresh", (JsonElement _) =>
{
	var payload = new { status = "ok", cached = false };
	var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	});
	return Results.Text(json, "application/json");
});

app.Run();

// Expose Program class for WebApplicationFactory in tests
public partial class Program { }

// Simple in-memory metrics aggregation
public static class AppMetrics
{
	public static long TotalRequests;
	public static long TotalErrors;
	public static long TotalRequestMs;
	public static ConcurrentDictionary<string, long> RequestsByPath { get; } = new();
	public static ConcurrentDictionary<int, long> StatusCounts { get; } = new();
}

// DelegatingHandler to propagate correlation header to outbound requests
public sealed class CorrelationPropagationHandler : DelegatingHandler
{
	private readonly IHttpContextAccessor _http;
	private const string HeaderName = "X-Correlation-ID";

	public CorrelationPropagationHandler(IHttpContextAccessor http)
	{
		_http = http;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var corrId = _http.HttpContext?.Response.Headers[HeaderName].FirstOrDefault()
			?? _http.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();
		if (!string.IsNullOrWhiteSpace(corrId))
		{
			request.Headers.Remove(HeaderName);
			request.Headers.TryAddWithoutValidation(HeaderName, corrId!);
		}
		return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
	}
}
