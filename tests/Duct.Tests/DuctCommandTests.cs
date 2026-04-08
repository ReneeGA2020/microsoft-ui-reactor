using Duct.Core;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for DuctCommand and DuctCommand&lt;T&gt; records — equality, with expressions, IsEnabled logic.
/// Pure C# record tests, no WinUI thread needed.
/// </summary>
public class DuctCommandTests
{
    // ════════════════════════════════════════════════════════════════
    //  DuctCommand — structural equality
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DuctCommand_Structural_Equality()
    {
        var a = new DuctCommand { Label = "Save" };
        var b = new DuctCommand { Label = "Save" };
        Assert.Equal(a, b);
    }

    [Fact]
    public void DuctCommand_Inequality_When_Label_Differs()
    {
        var a = new DuctCommand { Label = "Save" };
        var b = new DuctCommand { Label = "Open" };
        Assert.NotEqual(a, b);
    }

    // ════════════════════════════════════════════════════════════════
    //  DuctCommand — with expression
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DuctCommand_With_Creates_Modified_Copy()
    {
        var original = new DuctCommand { Label = "Save", Description = "Save the file" };
        var modified = original with { Label = "Save As" };

        Assert.Equal("Save As", modified.Label);
        Assert.Equal("Save the file", modified.Description); // unchanged
        Assert.NotEqual(original, modified);
    }

    // ════════════════════════════════════════════════════════════════
    //  DuctCommand — IsEnabled logic
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void IsEnabled_False_When_CanExecute_False()
    {
        var cmd = new DuctCommand { Label = "Cut", CanExecute = false };
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void IsEnabled_False_When_IsExecuting_True()
    {
        var cmd = new DuctCommand { Label = "Save", IsExecuting = true };
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void IsEnabled_False_When_CanExecute_False_And_IsExecuting_True()
    {
        var cmd = new DuctCommand { Label = "Save", CanExecute = false, IsExecuting = true };
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void IsEnabled_True_When_CanExecute_True_And_IsExecuting_False()
    {
        var cmd = new DuctCommand { Label = "Save" };
        Assert.True(cmd.CanExecute);
        Assert.False(cmd.IsExecuting);
        Assert.True(cmd.IsEnabled);
    }

    // ════════════════════════════════════════════════════════════════
    //  DuctCommand — defaults
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DuctCommand_Defaults()
    {
        var cmd = new DuctCommand { Label = "Test" };
        Assert.True(cmd.CanExecute);
        Assert.False(cmd.IsExecuting);
        Assert.True(cmd.IsEnabled);
        Assert.Null(cmd.Execute);
        Assert.Null(cmd.ExecuteAsync);
        Assert.Null(cmd.Icon);
        Assert.Null(cmd.Description);
        Assert.Null(cmd.Accelerator);
        Assert.Null(cmd.AccessKey);
    }

    // ════════════════════════════════════════════════════════════════
    //  DuctCommand<T> — equality and IsEnabled
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DuctCommandT_Structural_Equality()
    {
        var a = new DuctCommand<string> { Label = "Delete" };
        var b = new DuctCommand<string> { Label = "Delete" };
        Assert.Equal(a, b);
    }

    [Fact]
    public void DuctCommandT_IsEnabled_Matches_DuctCommand_Logic()
    {
        var enabled = new DuctCommand<int> { Label = "Select" };
        Assert.True(enabled.IsEnabled);

        var cantExecute = new DuctCommand<int> { Label = "Select", CanExecute = false };
        Assert.False(cantExecute.IsEnabled);

        var executing = new DuctCommand<int> { Label = "Select", IsExecuting = true };
        Assert.False(executing.IsEnabled);

        var both = new DuctCommand<int> { Label = "Select", CanExecute = false, IsExecuting = true };
        Assert.False(both.IsEnabled);
    }

    [Fact]
    public void DuctCommandT_With_Creates_Modified_Copy()
    {
        var original = new DuctCommand<string> { Label = "Delete", Description = "Remove item" };
        var modified = original with { Label = "Remove" };

        Assert.Equal("Remove", modified.Label);
        Assert.Equal("Remove item", modified.Description);
    }
}
