using TestWarden.Quarantine;

namespace TestWarden.Tests.Unit.Quarantine;

public class QuarantineListFileTests
{
    [Fact]
    public void Parse_ShouldSkipCommentsAndBlankLines_WhenGivenAList()
    {
        string[] lines = ["# flaky on Linux", "", "  Shop.OrderTests.Bills  ", "Shop.CartTests.*", "   # indented comment"];

        var entries = QuarantineListFile.Parse(lines);

        Assert.Equal(["Shop.OrderTests.Bills", "Shop.CartTests.*"], entries.Select(e => e.FullName));
    }

    [Fact]
    public void Parse_ShouldDropArguments_WhenGivenATheoryCase()
    {
        var entries = QuarantineListFile.Parse(["Shop.OrderTests.Rounds(x: 1)"]);

        Assert.Equal(["Shop.OrderTests.Rounds"], entries.Select(e => e.FullName));
    }

    [Fact]
    public void Read_ShouldThrow_WhenGivenAMissingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

        Assert.Throws<QuarantineException>(() => QuarantineListFile.Read(path));
    }
}
