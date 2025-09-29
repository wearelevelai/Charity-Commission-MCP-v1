using System.Text.Json;
using System.Text;

// Simple JSON Schema summarizer -> Markdown
// Usage: dotnet run --project tools/SchemaDocGen <schemasDir> <outputFile>

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: SchemaDocGen <schemasDir> <outputFile>");
    return 1;
}

var rootDir = args[0];
var outputFile = args[1];
if (!Directory.Exists(rootDir))
{
    Console.Error.WriteLine($"Schemas directory not found: {rootDir}");
    return 2;
}

var sb = new StringBuilder();
sb.AppendLine("# API Reference");
sb.AppendLine();
sb.AppendLine($"> Generated on {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
sb.AppendLine();

var files = Directory.GetFiles(rootDir, "*.schema.json", SearchOption.TopDirectoryOnly)
    .OrderBy(f => f)
    .ToArray();

foreach (var file in files)
{
    var name = Path.GetFileName(file);
    using var fs = File.OpenRead(file);
    using var doc = await JsonDocument.ParseAsync(fs);
    var root = doc.RootElement;
    var title = root.TryGetProperty("title", out var tEl) && tEl.ValueKind == JsonValueKind.String ? tEl.GetString() : null;
    var type = root.TryGetProperty("type", out var tyEl) && tyEl.ValueKind == JsonValueKind.String ? tyEl.GetString() : null;
    var required = new List<string>();
    if (root.TryGetProperty("required", out var reqEl) && reqEl.ValueKind == JsonValueKind.Array)
    {
        required.AddRange(reqEl.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)));
    }
    var props = new Dictionary<string, string?>();
    if (root.TryGetProperty("properties", out var propsEl) && propsEl.ValueKind == JsonValueKind.Object)
    {
        foreach (var prop in propsEl.EnumerateObject())
        {
            var pType = prop.Value.TryGetProperty("type", out var pTypeEl) && pTypeEl.ValueKind == JsonValueKind.String
                ? pTypeEl.GetString()
                : null;
            props[prop.Name] = pType;
        }
    }

    sb.AppendLine($"## {name}");
    if (!string.IsNullOrWhiteSpace(title)) sb.AppendLine($"- Title: {title}");
    if (!string.IsNullOrWhiteSpace(type)) sb.AppendLine($"- Type: {type}");
    if (required.Count > 0) sb.AppendLine($"- Required: {string.Join(", ", required)}");
    if (props.Count > 0)
    {
        sb.AppendLine("- Properties:");
        foreach (var kv in props)
        {
            sb.AppendLine($"  - {kv.Key}: {kv.Value ?? "(unknown)"}");
        }
    }
    sb.AppendLine();
}

Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
await File.WriteAllTextAsync(outputFile, sb.ToString());
Console.WriteLine($"Wrote API reference: {outputFile}");
return 0;
