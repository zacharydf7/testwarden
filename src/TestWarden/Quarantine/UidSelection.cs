namespace TestWarden.Quarantine;

// Splits the listed tests into the ones to run and the quarantined ones to skip.
public sealed record UidSelection(IReadOnlyList<ListedTest> Kept, IReadOnlyList<ListedTest> Skipped)
{
    // MTP drops every double quote from a response file, and NUnit puts string arguments into its UIDs.
    public bool FitsResponseFile => Kept.All(t => !t.Uid.Contains('"'));

    // An empty response file means no filter at all, so the caller must not run the app in that case.
    public bool NothingToRun => Kept.Count == 0;

    public static UidSelection Select(IReadOnlyList<ListedTest> tests, IReadOnlyList<QuarantineEntry> entries)
    {
        var kept = new List<ListedTest>();
        var skipped = new List<ListedTest>();
        foreach (var test in tests)
        {
            (entries.Any(e => e.Matches(test.FullName)) ? skipped : kept).Add(test);
        }

        return new UidSelection(kept, skipped);
    }

    // One "--filter-uid" per line keeps the command line short, whatever the test count.
    public void WriteResponseFile(string path)
    {
        if (NothingToRun)
        {
            throw new InvalidOperationException("No tests are left to run, and an empty response file would run them all.");
        }

        if (!FitsResponseFile)
        {
            throw new InvalidOperationException("A test UID contains a double quote, which a response file can't carry.");
        }

        File.WriteAllLines(path, Kept.Select(t => $"--filter-uid \"{t.Uid}\""));
    }
}
