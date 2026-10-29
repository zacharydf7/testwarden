using TestWarden.Quarantine;

namespace TestWarden.Tests.Unit.Quarantine;

public class FilterArgumentsTests
{
    [Fact]
    public void Split_ShouldSeparateFiltersFromOtherArguments_WhenGivenAMix()
    {
        var split = FilterArguments.Split(["--report-junit", "--filter-not-trait", "Category=Slow", "Category=Db", "--coverlet",
            "--filter-query", "/*/Shop"]);

        Assert.Equal(["--report-junit", "--coverlet"], split.Rest);
        Assert.Equal(["--filter-not-trait", "--filter-query"], split.Filters.Select(f => f.Name));
        Assert.Equal(["Category=Slow", "Category=Db"], split.Filters[0].Values);
        Assert.Equal(["/*/Shop"], split.Filters[1].Values);
    }

    [Fact]
    public void Split_ShouldReturnNoFilters_WhenGivenNone()
    {
        var split = FilterArguments.Split(["--results-directory", "out"]);

        Assert.Empty(split.Filters);
        Assert.Equal(["--results-directory", "out"], split.Rest);
    }

    [Fact]
    public void Split_ShouldKeepAnOptionWithoutValues_WhenItIsLast()
    {
        var split = FilterArguments.Split(["--filter"]);

        Assert.Equal([new FilterOption("--filter", [])], split.Filters, (a, b) => a.Name == b.Name && a.Values.Count == b.Values.Count);
    }
}
