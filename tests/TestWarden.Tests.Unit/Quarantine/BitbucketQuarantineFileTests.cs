using TestWarden.Quarantine;

namespace TestWarden.Tests.Unit.Quarantine;

public class BitbucketQuarantineFileTests
{
    [Fact]
    public void Parse_ShouldJoinPackageAndClass_WhenGivenAtlassiansSampleShape()
    {
        var entries = BitbucketQuarantineFile.Parse("""
            { "quarantinedTests": [ { "package": "Shop", "name": "Bills", "class": "OrderTests" } ] }
            """);

        Assert.Equal(["Shop.OrderTests.Bills"], entries.Select(e => e.FullName));
    }

    [Fact]
    public void Parse_ShouldUseTheClassAlone_WhenGivenNoPackage()
    {
        var entries = BitbucketQuarantineFile.Parse("""
            { "quarantinedTests": [ { "class": "Shop.OrderTests", "name": "Bills" }, { "package": "", "class": "Shop.CartTests", "name": "Adds" } ] }
            """);

        Assert.Equal(["Shop.OrderTests.Bills", "Shop.CartTests.Adds"], entries.Select(e => e.FullName));
    }

    [Fact]
    public void Parse_ShouldDropTheRepeatedClassAndArguments_WhenGivenAJUnitStyleName()
    {
        var entries = BitbucketQuarantineFile.Parse("""
            { "quarantinedTests": [ { "package": "Shop", "class": "OrderTests", "name": "Shop.OrderTests.Rounds(x: 1)" } ] }
            """);

        Assert.Equal(["Shop.OrderTests.Rounds"], entries.Select(e => e.FullName));
    }

    [Fact]
    public void Parse_ShouldReturnNoEntries_WhenGivenAnEmptyList()
    {
        var entries = BitbucketQuarantineFile.Parse("""{ "quarantinedTests": [] }""");

        Assert.Empty(entries);
    }

    [Theory]
    [InlineData("""{ "quarantined": [] }""")]
    [InlineData("""{ "quarantinedTests": [ """)]
    [InlineData("""{ "quarantinedTests": [ { "name": "Bills" } ] }""")]
    [InlineData("""{ "quarantinedTests": [ { "package": "Shop", "class": "OrderTests" } ] }""")]
    [InlineData("""{ "quarantinedTests": [ { "package": "Shop", "class": "", "name": "Bills" } ] }""")]
    [InlineData("""[]""")]
    [InlineData("""{ "quarantinedTests": {} }""")]
    [InlineData("""{ "quarantinedTests": [ "Shop.OrderTests.Bills" ] }""")]
    public void Parse_ShouldThrow_WhenGivenAFileItCantUse(string json)
    {
        Assert.Throws<QuarantineException>(() => BitbucketQuarantineFile.Parse(json));
    }

    [Fact]
    public void Read_ShouldThrow_WhenGivenAMissingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        var exception = Assert.Throws<QuarantineException>(() => BitbucketQuarantineFile.Read(path));

        Assert.Contains(path, exception.Message);
    }
}
