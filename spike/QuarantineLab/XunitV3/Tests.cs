namespace QuarantineLab;

public class OrderTests
{
    [Fact]
    public void Ships() { }

    [Fact]
    public void Bills() { }

    [Fact]
    public void BillsLater() { }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Rounds(int x) => Assert.True(x > 0);

    [Fact]
    public void Refunds() { }
}

public class CartTests
{
    [Fact]
    public void Adds() { }

    [Fact]
    public void Removes() { }

    [Fact]
    [Trait("Category", "Slow")]
    public void Empties() { }
}
