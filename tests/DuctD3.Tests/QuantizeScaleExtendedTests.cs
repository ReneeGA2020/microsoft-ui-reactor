// Extended tests for QuantizeScale, QuantileScale, ThresholdScale — covers mapping edge cases,
// InvertExtent roundtrips, NaN handling, copy independence, and boundary behavior
// not fully exercised in ScaleTests.cs

using Xunit;

namespace Duct.D3.Tests;

public class QuantizeScaleExtendedTests
{
    [Fact]
    public void Map_QuantizesToCorrectSegment()
    {
        var s = new QuantizeScale().SetDomain(0, 10).SetRange(1, 2, 3, 4);
        Assert.Equal(1, s.Map(1));   // [0, 2.5) → 1
        Assert.Equal(2, s.Map(3));   // [2.5, 5) → 2
        Assert.Equal(3, s.Map(6));   // [5, 7.5) → 3
        Assert.Equal(4, s.Map(9));   // [7.5, 10] → 4
    }

    [Fact]
    public void Map_ClampsBelowDomain()
    {
        var s = new QuantizeScale().SetDomain(0, 10).SetRange(1, 2, 3);
        Assert.Equal(1, s.Map(-5)); // below domain → first range value
    }

    [Fact]
    public void Map_ClampsAboveDomain()
    {
        var s = new QuantizeScale().SetDomain(0, 10).SetRange(1, 2, 3);
        Assert.Equal(3, s.Map(15)); // above domain → last range value
    }

    [Fact]
    public void Map_NaN_ReturnsNaN()
    {
        var s = new QuantizeScale().SetDomain(0, 10).SetRange(1, 2, 3);
        Assert.True(double.IsNaN(s.Map(double.NaN)));
    }

    [Fact]
    public void InvertExtent_Roundtrip()
    {
        var s = new QuantizeScale().SetDomain(0, 100).SetRange(1, 2, 3, 4);
        // Map a value, then invert the result to get the extent
        double mapped = s.Map(60); // should be 3
        Assert.Equal(3, mapped);
        var (x0, x1) = s.InvertExtent(mapped);
        Assert.True(x0 <= 60 && 60 <= x1);
    }

    [Fact]
    public void InvertExtent_UnknownRangeValue_ReturnsNaN()
    {
        var s = new QuantizeScale().SetDomain(0, 10).SetRange(1, 2, 3);
        var (x0, x1) = s.InvertExtent(999);
        Assert.True(double.IsNaN(x0));
        Assert.True(double.IsNaN(x1));
    }

    [Fact]
    public void Thresholds_CountIsRangeLengthMinusOne()
    {
        var s = new QuantizeScale().SetDomain(0, 100).SetRange(1, 2, 3, 4, 5);
        var thresholds = s.Thresholds();
        Assert.Equal(4, thresholds.Length);
        // Thresholds should be evenly spaced
        Assert.InRange(thresholds[0], 19.999, 20.001);
        Assert.InRange(thresholds[1], 39.999, 40.001);
        Assert.InRange(thresholds[2], 59.999, 60.001);
        Assert.InRange(thresholds[3], 79.999, 80.001);
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s = new QuantizeScale().SetDomain(0, 10).SetRange(1, 2, 3);
        var copy = s.Copy();

        copy.SetDomain(0, 1000);
        Assert.Equal([0.0, 10.0], s.Domain);
        Assert.Equal([0.0, 1000.0], copy.Domain);
    }

    [Fact]
    public void Domain_GetReturnsCurrentBounds()
    {
        var s = new QuantizeScale().SetDomain(5, 15);
        Assert.Equal([5.0, 15.0], s.Domain);
    }
}

public class QuantileScaleExtendedTests
{
    [Fact]
    public void Map_SortedData_CorrectQuantiles()
    {
        // 10 values split into 4 quartiles
        var s = new QuantileScale()
            .SetDomain(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
            .SetRange(10, 20, 30, 40);
        Assert.Equal(10, s.Map(1));
        Assert.Equal(40, s.Map(10));
        // Midpoint should be in middle quartile
        double mid = s.Map(5);
        Assert.True(mid == 20 || mid == 30);
    }

    [Fact]
    public void Map_UnsortedDomain_StillWorks()
    {
        // Domain is sorted internally
        var s = new QuantileScale()
            .SetDomain(10, 1, 5, 3, 8, 2, 7, 4, 9, 6)
            .SetRange(0, 1, 2, 3);
        // Should behave the same as sorted domain
        var s2 = new QuantileScale()
            .SetDomain(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
            .SetRange(0, 1, 2, 3);
        Assert.Equal(s2.Map(5), s.Map(5));
    }

    [Fact]
    public void Quantiles_ReturnsThresholdValues()
    {
        var s = new QuantileScale()
            .SetDomain(3, 6, 7, 8, 8, 10, 13, 15, 16, 20)
            .SetRange(0, 1, 2, 3);
        var q = s.Quantiles();
        Assert.Equal(3, q.Length); // 4 range values → 3 thresholds
    }

    [Fact]
    public void InvertExtent_ReturnsCorrectInterval()
    {
        var s = new QuantileScale()
            .SetDomain(3, 6, 7, 8, 8, 10, 13, 15, 16, 20)
            .SetRange(0, 1, 2, 3);
        var (x0, _) = s.InvertExtent(0);
        // First quantile extent starts at domain minimum
        Assert.Equal(3, x0);
    }

    [Fact]
    public void InvertExtent_LastQuantile_EndsAtDomainMax()
    {
        var s = new QuantileScale()
            .SetDomain(3, 6, 7, 8, 8, 10, 13, 15, 16, 20)
            .SetRange(0, 1, 2, 3);
        var (_, x1) = s.InvertExtent(3);
        Assert.Equal(20, x1);
    }

    [Fact]
    public void InvertExtent_UnknownRangeValue_ReturnsNaN()
    {
        var s = new QuantileScale()
            .SetDomain(1, 2, 3, 4, 5)
            .SetRange(0, 1);
        var (x0, x1) = s.InvertExtent(999);
        Assert.True(double.IsNaN(x0));
        Assert.True(double.IsNaN(x1));
    }

    [Fact]
    public void Map_NaN_ReturnsNaN()
    {
        var s = new QuantileScale()
            .SetDomain(1, 2, 3, 4, 5)
            .SetRange(0, 1);
        Assert.True(double.IsNaN(s.Map(double.NaN)));
    }

    [Fact]
    public void Domain_NaNValuesFiltered()
    {
        var s = new QuantileScale()
            .SetDomain(1, double.NaN, 3, double.NaN, 5)
            .SetRange(0, 1);
        // NaN values should be filtered out
        Assert.Equal(3, s.Domain.Length);
        Assert.Equal([1.0, 3.0, 5.0], s.Domain);
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s = new QuantileScale()
            .SetDomain(1, 2, 3, 4, 5)
            .SetRange(0, 1);
        var copy = s.Copy();

        copy.SetDomain(10, 20, 30, 40, 50);
        Assert.Equal([1.0, 2.0, 3.0, 4.0, 5.0], s.Domain);
    }

    [Fact]
    public void InvertExtent_EmptyDomain_ReturnsNaN()
    {
        var s = new QuantileScale().SetRange(0, 1);
        var (x0, x1) = s.InvertExtent(0);
        Assert.True(double.IsNaN(x0));
        Assert.True(double.IsNaN(x1));
    }
}

public class ThresholdScaleExtendedTests
{
    [Fact]
    public void Map_CustomThresholds_CorrectBuckets()
    {
        var s = new ThresholdScale().SetDomain(10, 20, 30).SetRange(0, 1, 2, 3);
        Assert.Equal(0, s.Map(5));   // below 10
        Assert.Equal(1, s.Map(10));  // at first threshold → goes to next bucket (bisectRight)
        Assert.Equal(1, s.Map(15));  // between 10 and 20
        Assert.Equal(2, s.Map(25));  // between 20 and 30
        Assert.Equal(3, s.Map(35));  // above 30
    }

    [Fact]
    public void Map_NaN_ReturnsNaN()
    {
        var s = new ThresholdScale().SetDomain(10, 20).SetRange(0, 1, 2);
        Assert.True(double.IsNaN(s.Map(double.NaN)));
    }

    [Fact]
    public void InvertExtent_FirstBucket_NegativeInfinity()
    {
        var s = new ThresholdScale().SetDomain(10, 20).SetRange(0, 1, 2);
        var (x0, _) = s.InvertExtent(0);
        Assert.True(double.IsNegativeInfinity(x0));
    }

    [Fact]
    public void InvertExtent_LastBucket_PositiveInfinity()
    {
        var s = new ThresholdScale().SetDomain(10, 20).SetRange(0, 1, 2);
        var (_, x1) = s.InvertExtent(2);
        Assert.True(double.IsPositiveInfinity(x1));
    }

    [Fact]
    public void InvertExtent_MiddleBucket_ReturnsThresholds()
    {
        var s = new ThresholdScale().SetDomain(10, 20).SetRange(0, 1, 2);
        var (x0, x1) = s.InvertExtent(1);
        Assert.Equal(10, x0);
        Assert.Equal(20, x1);
    }

    [Fact]
    public void InvertExtent_UnknownRangeValue_ReturnsNaN()
    {
        var s = new ThresholdScale().SetDomain(10, 20).SetRange(0, 1, 2);
        var (x0, x1) = s.InvertExtent(999);
        Assert.True(double.IsNaN(x0));
        Assert.True(double.IsNaN(x1));
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s = new ThresholdScale().SetDomain(10, 20).SetRange(0, 1, 2);
        var copy = s.Copy();

        copy.SetDomain(100, 200);
        Assert.Equal([10.0, 20.0], s.Domain);
        Assert.Equal([100.0, 200.0], copy.Domain);
    }

    [Fact]
    public void Default_HasSingleThreshold()
    {
        var s = new ThresholdScale();
        Assert.Equal([0.5], s.Domain);
        Assert.Equal([0.0, 1.0], s.Range);
    }
}
