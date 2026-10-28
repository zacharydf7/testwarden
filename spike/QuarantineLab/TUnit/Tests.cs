namespace QuarantineLab;

public class OrderTests
{
    [Test]
    public void Ships() { }

    [Test]
    public void Bills() { }

    [Test]
    public void BillsLater() { }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Rounds(int x) => await Assert.That(x).IsGreaterThan(0);

    [Test]
    public void Refunds() { }
}

public class CartTests
{
    [Test]
    public void Adds() { }

    [Test]
    public void Removes() { }

    [Test]
    [Category("Slow")]
    public void Empties() { }
}
