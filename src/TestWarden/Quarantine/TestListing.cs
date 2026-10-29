using System.Text;
using System.Text.Json;

namespace TestWarden.Quarantine;

public sealed record ListedTest(string Uid, string FullName);

// Reads the output of "--list-tests json" (MTP 2.3 and later), which may follow a framework banner.
public static class TestListing
{
    public static List<ListedTest> Parse(string output)
    {
        var start = output.IndexOf('{');
        if (start < 0)
        {
            throw new QuarantineException("The test app printed no JSON test list.");
        }

        // Read one JSON value and ignore anything the app prints after it.
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(output[start..]));
        JsonDocument document;
        try
        {
            document = JsonDocument.ParseValue(ref reader);
        }
        catch (JsonException exception)
        {
            throw new QuarantineException($"The test app's JSON test list isn't valid: {exception.Message}");
        }

        using var _ = document;
        var tests = new List<ListedTest>();
        try
        {
            foreach (var test in document.RootElement.GetProperty("tests").EnumerateArray())
            {
                tests.Add(new ListedTest(test.GetProperty("uid").GetString()!, FullName(test)));
            }
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException)
        {
            // JsonElement throws these for a missing property or a value of the wrong kind.
            throw new QuarantineException($"The test app's JSON test list doesn't have the expected shape: {exception.Message}");
        }

        return tests;
    }

    private static string FullName(JsonElement test)
    {
        if (!test.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.Object)
        {
            throw new QuarantineException($"The test app listed '{test.GetProperty("uid").GetString()}' without its method name.");
        }

        var ns = type.GetProperty("namespace").GetString();
        var name = $"{type.GetProperty("typeName").GetString()}.{type.GetProperty("methodName").GetString()}";
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }
}
