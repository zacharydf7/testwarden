using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize]

namespace QuarantineLab;

[TestClass]
public class OrderTests
{
    [TestMethod]
    public void Ships() { }

    [TestMethod]
    public void Bills() { }

    [TestMethod]
    public void BillsLater() { }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void Rounds(int x) => Assert.IsGreaterThan(0, x);

    [TestMethod]
    public void Refunds() { }
}

[TestClass]
public class CartTests
{
    [TestMethod]
    public void Adds() { }

    [TestMethod]
    public void Removes() { }

    [TestMethod]
    [TestCategory("Slow")]
    public void Empties() { }
}
