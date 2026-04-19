// Port of d3-shape/test/stack-test.js (subset — reactor1's StackGenerator
// implements the 'none' offset only; order and other offsets aren't ported.)

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class StackTests
{
    private record Row(double A, double B, double C);

    private static StackGenerator<Row> MakeStack()
    {
        return StackGenerator.Create<Row>()
            .SetKeys("a", "b", "c")
            .SetValue((row, key) => key switch
            {
                "a" => row.A,
                "b" => row.B,
                "c" => row.C,
                _ => 0,
            });
    }

    [Fact]
    public void Stack_Produces_Series_Per_Key()
    {
        var data = new[] { new Row(1, 2, 3), new Row(4, 5, 6) };
        var series = MakeStack().Generate(data);
        Assert.Equal(3, series.Length);
        Assert.Equal("a", series[0].Key);
        Assert.Equal("b", series[1].Key);
        Assert.Equal("c", series[2].Key);
    }

    [Fact]
    public void Stack_Each_Series_Has_Point_Per_Row()
    {
        var data = new[] { new Row(1, 2, 3), new Row(4, 5, 6), new Row(7, 8, 9) };
        var series = MakeStack().Generate(data);
        Assert.All(series, s => Assert.Equal(3, s.Points.Length));
    }

    [Fact]
    public void Stack_First_Series_Starts_At_Zero()
    {
        var data = new[] { new Row(10, 20, 30) };
        var series = MakeStack().Generate(data);
        Assert.Equal(0, series[0].Points[0].Y0);
        Assert.Equal(10, series[0].Points[0].Y1);
    }

    [Fact]
    public void Stack_Second_Series_Starts_Where_First_Ends()
    {
        var data = new[] { new Row(10, 20, 30) };
        var series = MakeStack().Generate(data);
        Assert.Equal(10, series[1].Points[0].Y0);
        Assert.Equal(30, series[1].Points[0].Y1);
    }

    [Fact]
    public void Stack_Third_Series_Stacks_On_Top_Of_Second()
    {
        var data = new[] { new Row(10, 20, 30) };
        var series = MakeStack().Generate(data);
        Assert.Equal(30, series[2].Points[0].Y0);
        Assert.Equal(60, series[2].Points[0].Y1);
    }

    [Fact]
    public void Stack_Cumulative_Sum_Equals_Total()
    {
        // For each row, the top of the last series should equal the row sum.
        var data = new[]
        {
            new Row(1, 2, 3),
            new Row(10, 20, 30),
            new Row(100, 200, 300),
        };
        var series = MakeStack().Generate(data);
        for (int j = 0; j < data.Length; j++)
        {
            double total = data[j].A + data[j].B + data[j].C;
            Assert.Equal(total, series[2].Points[j].Y1, 10);
        }
    }

    [Fact]
    public void Stack_Empty_Data_Gives_Empty_Point_Arrays()
    {
        var series = MakeStack().Generate(Array.Empty<Row>());
        Assert.All(series, s => Assert.Empty(s.Points));
    }

    [Fact]
    public void Stack_No_Keys_Produces_No_Series()
    {
        var gen = StackGenerator.Create<Row>();
        var series = gen.Generate(new[] { new Row(1, 2, 3) });
        Assert.Empty(series);
    }

    [Fact]
    public void Stack_Single_Key_Behaves_Like_Raw_Values()
    {
        var gen = StackGenerator.Create<Row>()
            .SetKeys("a")
            .SetValue((row, _) => row.A);
        var data = new[] { new Row(5, 0, 0), new Row(7, 0, 0) };
        var series = gen.Generate(data);
        Assert.Single(series);
        Assert.Equal(5, series[0].Points[0].Y1);
        Assert.Equal(7, series[0].Points[1].Y1);
    }
}
