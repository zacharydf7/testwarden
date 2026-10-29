using TestWarden.Quarantine;

namespace TestWarden.Tests.Unit.Quarantine;

public class UidSelectionTests
{
    private static readonly ListedTest[] Tests =
    [
        new("u1", "Shop.OrderTests.Bills"),
        new("u2", "Shop.OrderTests.BillsLater"),
        new("u3", "Shop.OrderTests.Rounds"),
        new("u4", "Shop.OrderTests.Rounds"),
        new("u5", "Shop.CartTests.Adds"),
    ];

    [Fact]
    public void Select_ShouldSkipEveryCaseOfAQuarantinedMethod_WhenGivenExactNames()
    {
        var selection = UidSelection.Select(Tests, [new("Shop.OrderTests.Bills"), new("Shop.OrderTests.Rounds(x: 1)")]);

        Assert.Equal(["u2", "u5"], selection.Kept.Select(t => t.Uid));
        Assert.Equal(["u1", "u3", "u4"], selection.Skipped.Select(t => t.Uid));
    }

    [Fact]
    public void Select_ShouldSkipMatchingTests_WhenGivenAWildcard()
    {
        var selection = UidSelection.Select(Tests, [new("Shop.OrderTests.*")]);

        Assert.Equal(["u5"], selection.Kept.Select(t => t.Uid));
    }

    [Fact]
    public void Select_ShouldKeepEverything_WhenGivenNoEntries()
    {
        var selection = UidSelection.Select(Tests, []);

        Assert.Equal(5, selection.Kept.Count);
        Assert.Empty(selection.Skipped);
    }

    [Fact]
    public void NothingToRun_ShouldBeTrue_WhenEveryTestIsQuarantined()
    {
        var selection = UidSelection.Select(Tests, [new("Shop.*.*")]);

        Assert.True(selection.NothingToRun);
    }

    [Fact]
    public void FitsResponseFile_ShouldBeFalse_WhenAKeptUidHasAQuote()
    {
        var selection = UidSelection.Select([new("Shop.Strings.Echo(\"a b\")", "Shop.Strings.Echo")], []);

        Assert.False(selection.FitsResponseFile);
    }

    [Fact]
    public void WriteResponseFile_ShouldWriteOneQuotedUidPerLine_WhenCalled()
    {
        var selection = UidSelection.Select([new("u1", "Shop.A.B"), new("Shop.A.C(1, 2)", "Shop.A.C")], []);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".rsp");

        try
        {
            selection.WriteResponseFile(path);

            Assert.Equal(["--filter-uid \"u1\"", "--filter-uid \"Shop.A.C(1, 2)\""], File.ReadAllLines(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteResponseFile_ShouldThrow_WhenAKeptUidHasAQuote()
    {
        var selection = UidSelection.Select([new("Shop.Strings.Echo(\"a\")", "Shop.Strings.Echo")], []);

        Assert.Throws<InvalidOperationException>(() => selection.WriteResponseFile(UnusedPath()));
    }

    [Fact]
    public void WriteResponseFile_ShouldThrow_WhenNothingIsKept()
    {
        var selection = UidSelection.Select(Tests, [new("Shop.*.*")]);

        Assert.Throws<InvalidOperationException>(() => selection.WriteResponseFile(UnusedPath()));
    }

    private static string UnusedPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".rsp");
}
