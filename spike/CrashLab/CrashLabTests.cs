using SampleLibrary;

namespace CrashLab;

public class CrashLabTests
{
    [Fact]
    public void Test01() => Assert.Equal(1, SampleMath.Square(1));

    [Fact]
    public void Test02() => Assert.Equal(4, SampleMath.Square(2));

    [Fact]
    public void Test03() => Assert.Equal(9, SampleMath.Square(3));

    [Fact]
    public void Test04()
    {
        if (Scenario.CrashesMidRun)
        {
            NativeFault.Raise();
        }
    }

    [Fact]
    public void Test05() => Assert.Equal(25, SampleMath.Square(5));

    [Fact]
    public void Test06() => Assert.Equal(36, SampleMath.Square(6));

    [Fact]
    public void Test07() => Assert.False(Scenario.FailsOneTest, "failure-teardown-crash fails this test on purpose");

    [Fact]
    public void Test08() => Assert.Equal(64, SampleMath.Square(8));

    [Fact]
    public void Test09() => Assert.Equal(81, SampleMath.Square(9));

    [Fact]
    public void Test10() => Assert.Equal(100, SampleMath.Square(10));
}
