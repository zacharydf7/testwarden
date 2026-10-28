using System.Xml.Linq;

namespace Harness;

// S3: did coverlet.MTP still write a cobertura report that covers the product code?
public static class CoverageReport
{
    public static bool CoversSampleLibrary(string directory)
    {
        var report = Directory.GetFiles(directory, "*.cobertura*.xml").FirstOrDefault();
        if (report is null)
        {
            return false;
        }

        var package = XDocument.Load(report).Descendants("package").FirstOrDefault(p => (string?)p.Attribute("name") == "SampleLibrary");
        return package is not null && (double?)package.Attribute("line-rate") > 0;
    }
}
