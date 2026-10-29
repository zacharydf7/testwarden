using TestWarden.Quarantine;

namespace TestWarden.Tests.Unit.Quarantine;

public class QuarantineEntryTests
{
    [Fact]
    public void Constructor_ShouldDropArguments_WhenGivenATheoryCase()
    {
        var entry = new QuarantineEntry("Shop.OrderTests.Rounds(x: 1)");

        Assert.Equal("Shop.OrderTests.Rounds", entry.FullName);
    }

    [Fact]
    public void Constructor_ShouldSplitTheName_WhenGivenANestedNamespace()
    {
        var entry = new QuarantineEntry("Shop.Orders.OrderTests.Bills");

        Assert.Equal(("Shop.Orders", "OrderTests", "Bills"), (entry.Namespace, entry.ClassName, entry.MethodName));
    }

    [Fact]
    public void Constructor_ShouldLeaveTheNamespaceEmpty_WhenGivenAClassOutsideANamespace()
    {
        var entry = new QuarantineEntry("OrderTests.Bills");

        Assert.Equal(("", "OrderTests", "Bills"), (entry.Namespace, entry.ClassName, entry.MethodName));
    }

    [Theory]
    [InlineData("Bills")]
    [InlineData(".Bills")]
    [InlineData("Shop.OrderTests.")]
    [InlineData("Shop..Bills")]
    public void Constructor_ShouldThrow_WhenGivenANameWithAnEmptyPart(string name)
    {
        Assert.Throws<QuarantineException>(() => new QuarantineEntry(name));
    }

    [Theory]
    [InlineData("Shop.OrderTests.Bills", "Shop.OrderTests.Bills", true)]
    [InlineData("Shop.OrderTests.Bills", "Shop.OrderTests.BillsLater", false)]
    [InlineData("Shop.OrderTests.Bills", "Other.Shop.OrderTests.Bills", false)]
    [InlineData("Shop.OrderTests.*", "Shop.OrderTests.BillsLater", true)]
    [InlineData("Shop.*Tests.Bills", "Shop.CartTests.Bills", true)]
    [InlineData("Shop.OrderTests.Bill?", "Shop.OrderTests.Bills", true)]
    [InlineData("Shop.OrderTests.Bill?", "Shop.OrderTests.Bill", false)]
    [InlineData("Shop.OrderTests.Bills", "shop.ordertests.bills", false)]
    public void Matches_ShouldCompareWholeNames_WhenGivenATestName(string entry, string test, bool expected)
    {
        var matches = new QuarantineEntry(entry).Matches(test);

        Assert.Equal(expected, matches);
    }

    [Theory]
    [InlineData("Shop.OrderTests.Bills", false)]
    [InlineData("Shop.OrderTests.*", true)]
    [InlineData("Shop.OrderTests.Bill?", true)]
    public void HasWildcards_ShouldReportWildcards_WhenGivenAName(string name, bool expected)
    {
        Assert.Equal(expected, new QuarantineEntry(name).HasWildcards);
    }
}
