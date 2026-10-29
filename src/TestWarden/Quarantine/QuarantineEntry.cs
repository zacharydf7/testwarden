using System.IO.Enumeration;

namespace TestWarden.Quarantine;

// One quarantined test method as "Namespace.Class.Method". Entries from quarantine.txt may use * and ? wildcards.
public sealed record QuarantineEntry
{
    public QuarantineEntry(string fullName)
    {
        // Filters work on whole methods, so "Method(x: 1)" quarantines every case of that theory.
        FullName = fullName.Split('(')[0].Trim();
        if (!FullName.Contains('.') || FullName.Split('.').Any(part => part.Length == 0))
        {
            throw new QuarantineException($"'{fullName}' is not a fully qualified test method name.");
        }
    }

    public string FullName { get; }

    public bool HasWildcards => FullName.IndexOfAny(['*', '?']) >= 0;

    public string ClassFullName => FullName[..FullName.LastIndexOf('.')];

    public string Namespace => ClassFullName.Contains('.') ? ClassFullName[..ClassFullName.LastIndexOf('.')] : "";

    public string ClassName => ClassFullName[(ClassFullName.LastIndexOf('.') + 1)..];

    public string MethodName => FullName[(FullName.LastIndexOf('.') + 1)..];

    public bool Matches(string testFullName) => FileSystemName.MatchesSimpleExpression(FullName, testFullName, ignoreCase: false);
}
