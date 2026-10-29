using System.Text.Json;

namespace TestWarden.Quarantine;

// The file Bitbucket Pipelines writes to BITBUCKET_TEST_METADATA_FILE_PATH, as documented at
// https://support.atlassian.com/bitbucket-cloud/docs/quarantining-flaky-tests/.
public static class BitbucketQuarantineFile
{
    public static List<QuarantineEntry> Read(string path) =>
        File.Exists(path) ? Parse(File.ReadAllText(path)) : throw new QuarantineException($"Bitbucket's quarantine file '{path}' doesn't exist.");

    public static List<QuarantineEntry> Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new QuarantineException($"Bitbucket's quarantine file isn't valid JSON: {exception.Message}");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("quarantinedTests", out var tests) || tests.ValueKind != JsonValueKind.Array)
            {
                throw new QuarantineException("Bitbucket's quarantine file has no 'quarantinedTests' list.");
            }

            var entries = new List<QuarantineEntry>();
            foreach (var test in tests.EnumerateArray())
            {
                if (test.ValueKind != JsonValueKind.Object)
                {
                    throw new QuarantineException($"Bitbucket's quarantine file lists {test.ValueKind} '{test}' instead of a test.");
                }

                entries.Add(ToEntry(Text(test, "package"), Text(test, "class"), Text(test, "name")));
            }

            return entries;
        }
    }

    // JUnit names from .NET frameworks often repeat the class ("Ns.Class.Method"), so a repeated class is dropped.
    private static QuarantineEntry ToEntry(string package, string className, string name)
    {
        var fullClassName = package.Length == 0 ? className : $"{package}.{className}";
        var method = name.StartsWith(fullClassName + ".", StringComparison.Ordinal) ? name[(fullClassName.Length + 1)..] : name;
        return new QuarantineEntry($"{fullClassName}.{method}");
    }

    private static string Text(JsonElement test, string property) =>
        test.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
}
