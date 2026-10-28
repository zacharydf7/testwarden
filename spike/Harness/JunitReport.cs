using System.Xml;
using System.Xml.Linq;

namespace Harness;

// Proof from the JUnit format alone: the report is complete, every declared test is present, and none failed.
public static class JunitReport
{
    public static bool ProvesCleanRun(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (XmlException)
        {
            return false;
        }

        if (document.Root?.Name.LocalName != "testsuites")
        {
            return false;
        }

        var suites = document.Root.Elements("testsuite").ToList();
        var declared = suites.Sum(s => (int?)s.Attribute("tests") ?? 0);
        var failures = suites.Sum(s => ((int?)s.Attribute("failures") ?? 0) + ((int?)s.Attribute("errors") ?? 0));
        var testCases = document.Descendants("testcase").ToList();
        var failedCases = testCases.Count(t => t.Element("failure") is not null || t.Element("error") is not null);
        return declared > 0 && testCases.Count >= declared && failures == 0 && failedCases == 0;
    }
}
