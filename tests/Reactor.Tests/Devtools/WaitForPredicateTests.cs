using System.Text.Json;
using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

/// <summary>
/// JSON-to-IR parsing tests for the waitFor predicate. The live evaluation
/// against a real tree is covered by the self-host fixture (§2.17).
/// </summary>
public class WaitForPredicateTests
{
    [Fact]
    public void FromJson_AllFieldsPresent()
    {
        using var doc = JsonDocument.Parse("""
            {"selector":"#btn","textEquals":"OK","textMatches":"^[A-Z]+$","visible":true,"count":1}
            """);
        var p = WaitForPredicate.FromJson(doc.RootElement);
        Assert.Equal("#btn", p.Selector);
        Assert.Equal("OK", p.TextEquals);
        Assert.Equal("^[A-Z]+$", p.TextMatches);
        Assert.True(p.Visible);
        Assert.Equal(1, p.Count);
    }

    [Fact]
    public void FromJson_MissingFieldsAreNull()
    {
        using var doc = JsonDocument.Parse("""{"selector":"#btn"}""");
        var p = WaitForPredicate.FromJson(doc.RootElement);
        Assert.Equal("#btn", p.Selector);
        Assert.Null(p.TextEquals);
        Assert.Null(p.TextMatches);
        Assert.Null(p.Visible);
        Assert.Null(p.Count);
    }

    [Fact]
    public void FromJson_WrongTypeIgnored()
    {
        // Spec: missing/mis-typed predicate fields are ignored, not matched as false.
        using var doc = JsonDocument.Parse("""{"selector":"#btn","visible":"yes","count":"1"}""");
        var p = WaitForPredicate.FromJson(doc.RootElement);
        Assert.Null(p.Visible);
        Assert.Null(p.Count);
    }

    [Fact]
    public void FromJson_CountZero_Parses()
    {
        using var doc = JsonDocument.Parse("""{"selector":"#gone","count":0}""");
        var p = WaitForPredicate.FromJson(doc.RootElement);
        Assert.Equal(0, p.Count);
    }
}
