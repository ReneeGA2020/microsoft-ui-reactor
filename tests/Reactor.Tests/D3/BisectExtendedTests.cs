// Extended tests for D3Bisect — covers BisectRight, BisectLeft, BisectCenter
// with lo/hi parameters, empty arrays, and edge cases.

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class BisectExtendedTests
{
    // ─── BisectRight ────────────────────────────────────────────────

    [Fact]
    public void BisectRight_InsertsAfterEqual()
    {
        double[] arr = [1, 2, 3, 4, 5];
        Assert.Equal(3, D3Bisect.BisectRight(arr, 3));
    }

    [Fact]
    public void BisectRight_BeforeAll()
    {
        double[] arr = [10, 20, 30];
        Assert.Equal(0, D3Bisect.BisectRight(arr, 5));
    }

    [Fact]
    public void BisectRight_AfterAll()
    {
        double[] arr = [10, 20, 30];
        Assert.Equal(3, D3Bisect.BisectRight(arr, 35));
    }

    [Fact]
    public void BisectRight_EmptyArray()
    {
        double[] arr = [];
        Assert.Equal(0, D3Bisect.BisectRight(arr, 5));
    }

    [Fact]
    public void BisectRight_WithLoHi()
    {
        double[] arr = [1, 2, 3, 4, 5];
        // Search only in range [1, 3) of the array (elements 2, 3)
        Assert.Equal(2, D3Bisect.BisectRight(arr, 2, 1, 3));
    }

    [Fact]
    public void BisectRight_NaN_ReturnsHi()
    {
        double[] arr = [1, 2, 3];
        Assert.Equal(3, D3Bisect.BisectRight(arr, double.NaN));
    }

    [Fact]
    public void BisectRight_SingleElement()
    {
        double[] arr = [5];
        Assert.Equal(0, D3Bisect.BisectRight(arr, 3));
        Assert.Equal(1, D3Bisect.BisectRight(arr, 5));
        Assert.Equal(1, D3Bisect.BisectRight(arr, 7));
    }

    // ─── BisectLeft ─────────────────────────────────────────────────

    [Fact]
    public void BisectLeft_InsertsBeforeEqual()
    {
        double[] arr = [1, 2, 3, 4, 5];
        Assert.Equal(2, D3Bisect.BisectLeft(arr, 3));
    }

    [Fact]
    public void BisectLeft_BeforeAll()
    {
        double[] arr = [10, 20, 30];
        Assert.Equal(0, D3Bisect.BisectLeft(arr, 5));
    }

    [Fact]
    public void BisectLeft_AfterAll()
    {
        double[] arr = [10, 20, 30];
        Assert.Equal(3, D3Bisect.BisectLeft(arr, 35));
    }

    [Fact]
    public void BisectLeft_EmptyArray()
    {
        double[] arr = [];
        Assert.Equal(0, D3Bisect.BisectLeft(arr, 5));
    }

    [Fact]
    public void BisectLeft_WithLoHi()
    {
        double[] arr = [1, 2, 3, 4, 5];
        // Search only in range [2, 4) of the array (elements 3, 4)
        Assert.Equal(2, D3Bisect.BisectLeft(arr, 3, 2, 4));
    }

    [Fact]
    public void BisectLeft_NaN_ReturnsHi()
    {
        double[] arr = [1, 2, 3];
        Assert.Equal(3, D3Bisect.BisectLeft(arr, double.NaN));
    }

    [Fact]
    public void BisectLeft_Duplicates()
    {
        double[] arr = [1, 2, 2, 2, 3];
        Assert.Equal(1, D3Bisect.BisectLeft(arr, 2));
        Assert.Equal(4, D3Bisect.BisectRight(arr, 2));
    }

    // ─── BisectCenter ───────────────────────────────────────────────

    [Fact]
    public void BisectCenter_FindsNearest()
    {
        double[] arr = [0, 10, 20, 30];
        // 12 is closer to 10 than to 20
        Assert.Equal(1, D3Bisect.BisectCenter(arr, 12));
        // 18 is closer to 20 than to 10
        Assert.Equal(2, D3Bisect.BisectCenter(arr, 18));
    }

    [Fact]
    public void BisectCenter_ExactMatch()
    {
        double[] arr = [0, 10, 20, 30];
        Assert.Equal(1, D3Bisect.BisectCenter(arr, 10));
    }

    [Fact]
    public void BisectCenter_EmptyArray()
    {
        double[] arr = [];
        Assert.Equal(0, D3Bisect.BisectCenter(arr, 5));
    }

    [Fact]
    public void BisectCenter_SingleElement()
    {
        double[] arr = [10];
        Assert.Equal(0, D3Bisect.BisectCenter(arr, 5));
        Assert.Equal(0, D3Bisect.BisectCenter(arr, 15));
    }
}
