using System.Reflection;
using System.Text.Json;

namespace CCEW.Mcp.ContractTests.TestUtils;

public static class SchemaLoader
{
    public static string ReadText(string relativePathFromRepoRoot)
    {
        // Assuming tests run from repo root or project dir; normalize to repo root
        var repoRoot = FindRepoRoot();
        var fullPath = Path.Combine(repoRoot, relativePathFromRepoRoot);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Schema file not found: {fullPath}");
        }
        return File.ReadAllText(fullPath);
    }

    public static JsonDocument ReadJson(string relativePathFromRepoRoot)
    {
        var text = ReadText(relativePathFromRepoRoot);
        return JsonDocument.Parse(text);
    }

    private static string FindRepoRoot()
    {
        // Walk up from current directory until we find .git or specs folder we expect
        var dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6; i++)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")) || Directory.Exists(Path.Combine(dir, "specs")))
            {
                return dir;
            }
            var parent = Directory.GetParent(dir)?.FullName;
            if (parent is null) break;
            dir = parent;
        }
        // Fallback to current directory
        return Directory.GetCurrentDirectory();
    }
}
