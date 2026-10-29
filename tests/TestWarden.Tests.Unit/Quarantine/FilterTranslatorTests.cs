using TestWarden.Quarantine;

namespace TestWarden.Tests.Unit.Quarantine;

public class FilterTranslatorTests
{
    private const string XunitHelp = "Options:\n    --filter-class\n        Run a class.\n    --filter-not-method\n        Skip a method.\n";
    private const string VsTestHelp = "Options:\n    --filter\n        Filters tests using the given expression.\n    --filter-uid\n";
    private const string TreeNodeHelp = "Options:\n    --treenode-filter\n        Use a tree filter.\n    --filter-uid\n";

    private static readonly QuarantineEntry[] Entries =
        [new("Shop.OrderTests.Bills"), new("Shop.OrderTests.Rounds"), new("Shop.CartTests.Removes")];

    [Theory]
    [InlineData(XunitHelp, FilterFamily.XunitSimple)]
    [InlineData(VsTestHelp, FilterFamily.VsTest)]
    public void ChooseFamily_ShouldUseTheAppsFamily_WhenGivenNoUserFilter(string help, FilterFamily expected)
    {
        Assert.Equal(expected, FilterTranslator.ChooseFamily([], help));
    }

    [Theory]
    [InlineData("--filter-not-trait", FilterFamily.XunitSimple)]
    [InlineData("--filter-query", FilterFamily.XunitQuery)]
    [InlineData("--filter", FilterFamily.VsTest)]
    public void ChooseFamily_ShouldFollowTheUsersFilter_WhenGivenOne(string option, FilterFamily expected)
    {
        Assert.Equal(expected, FilterTranslator.ChooseFamily([new FilterOption(option, ["x"])], XunitHelp));
    }

    [Fact]
    public void ChooseFamily_ShouldThrow_WhenGivenTwoFamilies()
    {
        FilterOption[] filters = [new("--filter-class", ["Shop.OrderTests"]), new("--filter-query", ["/*/Shop"])];

        Assert.Throws<QuarantineException>(() => FilterTranslator.ChooseFamily(filters, XunitHelp));
    }

    [Theory]
    [InlineData("--treenode-filter")]
    [InlineData("--filter-uid")]
    public void ChooseFamily_ShouldThrow_WhenGivenAFilterThatCantCarryExclusions(string option)
    {
        Assert.Throws<QuarantineException>(() => FilterTranslator.ChooseFamily([new FilterOption(option, ["x"])], TreeNodeHelp));
    }

    [Fact]
    public void ChooseFamily_ShouldThrow_WhenGivenAnAppWithoutAnExcludingFilter()
    {
        Assert.Throws<QuarantineException>(() => FilterTranslator.ChooseFamily([], TreeNodeHelp));
    }

    [Fact]
    public void Translate_ShouldAddOneFilterNotMethodPerEntry_WhenGivenXunitSimple()
    {
        var arguments = FilterTranslator.Translate(FilterFamily.XunitSimple, Entries, [new("--filter-not-trait", ["Category=Slow"])]);

        Assert.Equal(["--filter-not-trait", "Category=Slow", "--filter-not-method", "Shop.OrderTests.Bills", "--filter-not-method",
            "Shop.OrderTests.Rounds", "--filter-not-method", "Shop.CartTests.Removes"], arguments);
    }

    [Fact]
    public void Translate_ShouldAndTheUsersFilter_WhenGivenVsTest()
    {
        var arguments = FilterTranslator.Translate(FilterFamily.VsTest, Entries, [new("--filter", ["TestCategory!=Slow"])]);

        Assert.Equal(["--filter", "(TestCategory!=Slow)&FullyQualifiedName!=Shop.OrderTests.Bills"
            + "&FullyQualifiedName!=Shop.OrderTests.Rounds&FullyQualifiedName!=Shop.CartTests.Removes"], arguments);
    }

    [Fact]
    public void Translate_ShouldExcludeByClassAndNamespace_WhenGivenXunitQueryWithoutUserQuery()
    {
        var arguments = FilterTranslator.Translate(FilterFamily.XunitQuery, Entries, []);

        Assert.Equal(["--filter-query", "/*/Shop/OrderTests/(!Bills)&(!Rounds)", "--filter-query", "/*/Shop/CartTests/(!Removes)",
            "--filter-query", "/*/Shop/(!OrderTests)&(!CartTests)/*", "--filter-query", "/*/(!Shop)/*/*"], arguments);
    }

    [Fact]
    public void Translate_ShouldMergeEachUserQuery_WhenGivenXunitQueryWithSegmentsAndTrait()
    {
        QuarantineEntry[] entries = [new("Shop.OrderTests.Bills")];

        var arguments = FilterTranslator.Translate(FilterFamily.XunitQuery, entries, [new("--filter-query", ["/Shop.Tests/Shop/(Order*)|(Cart*)/[Category!=Slow]"])]);

        Assert.Equal(["--filter-query", "/Shop.Tests/Shop/((Order*)|(Cart*))&(OrderTests)/(!Bills)/[Category!=Slow]",
            "--filter-query", "/Shop.Tests/Shop/((Order*)|(Cart*))&((!OrderTests))/*/[Category!=Slow]",
            "--filter-query", "/Shop.Tests/(Shop)&((!Shop))/(Order*)|(Cart*)/*/[Category!=Slow]"], arguments);
    }

    [Theory]
    [InlineData(FilterFamily.XunitSimple, "--filter-not-trait", "Category=Slow")]
    [InlineData(FilterFamily.XunitQuery, "--filter-query", "/[Category!=Slow]")]
    [InlineData(FilterFamily.VsTest, "--filter", "TestCategory!=Slow")]
    public void Translate_ShouldReturnTheUsersFilterUnchanged_WhenGivenNoEntries(FilterFamily family, string option, string value)
    {
        Assert.Equal([option, value], FilterTranslator.Translate(family, [], [new(option, [value])]));
    }

    [Theory]
    [InlineData(FilterFamily.XunitQuery)]
    [InlineData(FilterFamily.VsTest)]
    public void Translate_ShouldReturnNoArguments_WhenGivenNoEntriesAndNoUserFilter(FilterFamily family)
    {
        Assert.Empty(FilterTranslator.Translate(family, [], []));
    }

    [Theory]
    [InlineData("/*/*/*/*[Category!=Slow]", "/*/Shop/OrderTests/(!Bills)/[Category!=Slow]")]
    [InlineData("/*/Shop[Category!=Slow]", "/*/Shop/OrderTests/(!Bills)/[Category!=Slow]")]
    [InlineData("/[Category!=Slow]", "/*/Shop/OrderTests/(!Bills)/[Category!=Slow]")]
    public void Translate_ShouldDetachTheTrait_WhenGivenATraitInAnyPosition(string userQuery, string firstQuery)
    {
        var arguments = FilterTranslator.Translate(FilterFamily.XunitQuery, [new("Shop.OrderTests.Bills")], [new("--filter-query", [userQuery])]);

        Assert.Equal(firstQuery, arguments[1]);
    }

    [Fact]
    public void Translate_ShouldCombineEveryUserQueryWithEveryExclusion_WhenGivenSeveralUserQueries()
    {
        var arguments = FilterTranslator.Translate(FilterFamily.XunitQuery, [new("Shop.OrderTests.Bills")],
            [new("--filter-query", ["/*/*/OrderTests", "/*/*/CartTests"])]);

        Assert.Equal(["--filter-query", "/*/Shop/OrderTests/(!Bills)", "--filter-query", "/*/Shop/(OrderTests)&((!OrderTests))/*",
            "--filter-query", "/*/(!Shop)/OrderTests/*", "--filter-query", "/*/Shop/(CartTests)&(OrderTests)/(!Bills)",
            "--filter-query", "/*/Shop/(CartTests)&((!OrderTests))/*", "--filter-query", "/*/(!Shop)/CartTests/*"], arguments);
    }

    [Fact]
    public void Translate_ShouldThrow_WhenGivenAWildcardEntry()
    {
        Assert.Throws<QuarantineException>(() => FilterTranslator.Translate(FilterFamily.XunitSimple, [new("Shop.OrderTests.*")], []));
    }

    [Fact]
    public void Translate_ShouldThrow_WhenGivenXunitQueryForATestOutsideANamespace()
    {
        Assert.Throws<QuarantineException>(() => FilterTranslator.Translate(FilterFamily.XunitQuery, [new("OrderTests.Bills")], []));
    }

    [Fact]
    public void Translate_ShouldThrow_WhenGivenAQueryWithTooManySegments()
    {
        Assert.Throws<QuarantineException>(() => FilterTranslator.Translate(FilterFamily.XunitQuery, Entries, [new("--filter-query", ["/a/b/c/d/e"])]));
    }
}
