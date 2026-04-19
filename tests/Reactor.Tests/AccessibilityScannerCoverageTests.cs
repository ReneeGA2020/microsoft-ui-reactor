using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Targets the remaining AccessibilityScanner code paths: TabIndex gap
/// detection (A11Y_007), the GetChildren switch arms for the less common
/// container element types, and the export-directory-create branch.
/// </summary>
public class AccessibilityScannerCoverageTests
{
    [Fact]
    public void TabIndexGap_GreaterThanOne_Reports_A11Y_007()
    {
        var tree = VStack(
            Button("A").TabIndex(1),
            Button("B").TabIndex(5),
            Button("C").TabIndex(10));
        var findings = AccessibilityScanner.Scan(tree);
        Assert.Contains(findings, d => d.Id == "A11Y_007");
    }

    [Fact]
    public void TabIndices_Sequential_No_Gap_Report()
    {
        var tree = VStack(
            Button("A").TabIndex(1),
            Button("B").TabIndex(2),
            Button("C").TabIndex(3));
        var findings = AccessibilityScanner.Scan(tree);
        Assert.DoesNotContain(findings, d => d.Id == "A11Y_007");
    }

    [Fact]
    public void Scanner_Walks_Border_Viewbox_And_Other_Single_Child_Containers()
    {
        // Each container exercises a different switch arm in GetChildren().
        var tree = VStack(
            Border(Button("InsideBorder")),
            ScrollView(Button("InsideScroll")),
            Viewbox(Button("InsideViewbox")),
            Expander("Hd", Button("InsideExpander")),
            Popup(Button("InsidePopup")),
            new GroupElement([Button("InGroup")])
        );
        // Just ensure scanning doesn't throw and returns some findings.
        var findings = AccessibilityScanner.Scan(tree);
        Assert.NotNull(findings);
    }

    [Fact]
    public void Scanner_Walks_RelativePanel_And_Canvas_Children()
    {
        var tree = VStack(
            RelativePanel(Button("InRP1"), Button("InRP2")),
            Canvas(Button("OnCanvas"))
        );
        var findings = AccessibilityScanner.Scan(tree);
        Assert.NotNull(findings);
    }

    [Fact]
    public void Scanner_Walks_ListView_And_GridView_Items()
    {
        var tree = VStack(
            ListView(Button("LV1"), Button("LV2")),
            GridView(Button("GV1"), Button("GV2"))
        );
        var findings = AccessibilityScanner.Scan(tree);
        Assert.NotNull(findings);
    }

    [Fact]
    public void ExportJson_Creates_Missing_Directory()
    {
        var dir = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), $"reactor-a11y-{Guid.NewGuid():N}");
        var path = global::System.IO.Path.Combine(dir, "report.json");
        try
        {
            var findings = AccessibilityScanner.Scan(VStack(Button("UnnamedIconBtn")));
            AccessibilityScanner.ExportJson(findings, path);
            Assert.True(global::System.IO.File.Exists(path));
            Assert.True(global::System.IO.Directory.Exists(dir));
        }
        finally
        {
            try { global::System.IO.File.Delete(path); } catch { }
            try { global::System.IO.Directory.Delete(dir); } catch { }
        }
    }

    [Fact]
    public void Empty_Element_Skipped_By_Walker()
    {
        // EmptyElement and null children should be skipped without throwing.
        var tree = VStack(Button("X"), new EmptyElement());
        var findings = AccessibilityScanner.Scan(tree);
        Assert.NotNull(findings);
    }
}
