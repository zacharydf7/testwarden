using System.Text.Json;

namespace QuarantineCheck;

public sealed record QuarantineEntry(string Namespace, string Class, string Method)
{
    public string FullClassName => Namespace.Length == 0 ? Class : $"{Namespace}.{Class}";

    public string FullName => $"{FullClassName}.{Method}";

    // Format from https://support.atlassian.com/bitbucket-cloud/docs/quarantining-flaky-tests/, whose sample splits the
    // class into package and class; JUnit names may also repeat the class and add arguments.
    public static QuarantineEntry FromBitbucket(string? package, string className, string name)
    {
        var fullClassName = string.IsNullOrEmpty(package) ? className : $"{package}.{className}";
        var method = name.Split('(')[0].Trim();
        if (method.StartsWith(fullClassName + ".", StringComparison.Ordinal))
        {
            method = method[(fullClassName.Length + 1)..];
        }

        var lastDot = fullClassName.LastIndexOf('.');
        return lastDot < 0
            ? new QuarantineEntry("", fullClassName, method)
            : new QuarantineEntry(fullClassName[..lastDot], fullClassName[(lastDot + 1)..], method);
    }

    public static List<QuarantineEntry> ReadBitbucketFile(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var entries = new List<QuarantineEntry>();
        foreach (var test in document.RootElement.GetProperty("quarantinedTests").EnumerateArray())
        {
            var package = test.TryGetProperty("package", out var value) ? value.GetString() : null;
            entries.Add(FromBitbucket(package, test.GetProperty("class").GetString()!, test.GetProperty("name").GetString()!));
        }

        return entries;
    }
}
