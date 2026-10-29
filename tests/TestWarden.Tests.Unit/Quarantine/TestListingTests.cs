using TestWarden.Quarantine;

namespace TestWarden.Tests.Unit.Quarantine;

public class TestListingTests
{
    private const string Listing = """
        xUnit.net v3 Microsoft.Testing.Platform v2 Runner v3.2.2 (64-bit .NET 10.0.14)

        {
          "schemaVersion": 1,
          "tests": [
            { "uid": "a1", "displayName": "Shop.OrderTests.Bills",
              "type": { "namespace": "Shop", "typeName": "OrderTests", "methodName": "Bills" } },
            { "uid": "b2", "displayName": "Adds",
              "type": { "namespace": "", "typeName": "CartTests", "methodName": "Adds" } }
          ]
        }
        Test discovery summary: found 2 test(s)
        """;

    [Fact]
    public void Parse_ShouldReadEachTest_WhenGivenListingWithBannerAndSummary()
    {
        var tests = TestListing.Parse(Listing);

        Assert.Equal([new ListedTest("a1", "Shop.OrderTests.Bills"), new ListedTest("b2", "CartTests.Adds")], tests);
    }

    [Theory]
    [InlineData("Option '--list-tests' expects no arguments")]
    [InlineData("""{ "tests": [ """)]
    [InlineData("""{ "tests": [ { "uid": "a1", "displayName": "Bills" } ] }""")]
    [InlineData("""{ "schemaVersion": 1 }""")]
    [InlineData("""{ "tests": [ { "type": { "namespace": "Shop", "typeName": "OrderTests", "methodName": "Bills" } } ] }""")]
    [InlineData("""{ "tests": [ { "uid": "a1", "type": { "namespace": "Shop", "typeName": "OrderTests" } } ] }""")]
    public void Parse_ShouldThrow_WhenGivenOutputItCantUse(string output)
    {
        Assert.Throws<QuarantineException>(() => TestListing.Parse(output));
    }
}
