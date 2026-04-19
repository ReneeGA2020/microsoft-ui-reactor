// Port of d3-chord tests

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class ChordTests
{
    [Fact]
    public void Chord_SquareMatrix()
    {
        var layout = new ChordLayout();
        double[][] matrix =
        [
            [11975, 5871, 8916, 2868],
            [1951, 10048, 2060, 6171],
            [8010, 16145, 8090, 8045],
            [1013, 990, 940, 6907],
        ];

        var data = layout.Generate(matrix);
        Assert.Equal(4, data.Groups.Length);
        Assert.True(data.Chords.Length > 0);
    }

    [Fact]
    public void Chord_GroupsSpanFullCircle()
    {
        var layout = new ChordLayout();
        double[][] matrix =
        [
            [10, 20],
            [30, 40],
        ];

        var data = layout.Generate(matrix);
        Assert.Equal(2, data.Groups.Length);

        Assert.Equal(0, data.Groups[0].StartAngle);

        double lastEnd = data.Groups[^1].EndAngle;
        Assert.True(lastEnd > 0);
    }

    [Fact]
    public void Chord_PadAngle()
    {
        var layout = new ChordLayout().SetPadAngle(0.1);
        double[][] matrix = [[10, 20], [30, 40]];

        var data = layout.Generate(matrix);
        double totalArc = 0;
        foreach (var g in data.Groups) totalArc += g.EndAngle - g.StartAngle;
        Assert.True(totalArc < 2 * Math.PI);
    }

    [Fact]
    public void Ribbon_GeneratesPath()
    {
        var layout = new ChordLayout();
        double[][] matrix = [[10, 20], [30, 40]];
        var data = layout.Generate(matrix);

        var ribbon = new RibbonGenerator().SetRadius(100);
        foreach (var chord in data.Chords)
        {
            var path = ribbon.Generate(chord);
            Assert.NotNull(path);
            Assert.StartsWith("M", path);
        }
    }
}
