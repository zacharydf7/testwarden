using NUnit.Framework;

namespace QuarantineLab;

public class OrderTests
{
    [Test]
    public void Ships() { }

    [Test]
    public void Bills() { }

    [Test]
    public void BillsLater() { }

    [TestCase(1)]
    [TestCase(2)]
    public void Rounds(int x) => Assert.That(x, Is.GreaterThan(0));

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
