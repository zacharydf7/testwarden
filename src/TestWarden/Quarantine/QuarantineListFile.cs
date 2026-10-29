namespace TestWarden.Quarantine;

// A checked-in quarantine.txt: one fully qualified test method per line, * and ? as wildcards, # starts a comment line.
// It is the format of MTP's Azure DevOps quarantine file, so one list can serve both.
public static class QuarantineListFile
{
    public static List<QuarantineEntry> Read(string path) =>
        File.Exists(path) ? Parse(File.ReadAllLines(path)) : throw new QuarantineException($"The quarantine list '{path}' doesn't exist.");

    public static List<QuarantineEntry> Parse(IEnumerable<string> lines)
    {
        var entries = new List<QuarantineEntry>();
        foreach (var line in lines.Select(l => l.Trim()))
        {
            if (line.Length > 0 && !line.StartsWith('#'))
            {
                entries.Add(new QuarantineEntry(line));
            }
        }

        return entries;
    }
}
