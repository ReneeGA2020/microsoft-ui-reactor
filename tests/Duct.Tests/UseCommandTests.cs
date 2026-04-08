using Duct.Core;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for UseCommand hook — sync passthrough, async lifecycle, re-entrance guards.
/// </summary>
public class UseCommandTests
{
    private static RenderContext CreateContext()
    {
        var ctx = new RenderContext();
        ctx.BeginRender(() => { });
        return ctx;
    }

    private static void Rerender(RenderContext ctx)
    {
        ctx.BeginRender(() => { });
    }

    // ════════════════════════════════════════════════════════════════
    //  Sync passthrough
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Sync_Command_Passes_Through_Unchanged()
    {
        var ctx = CreateContext();
        var original = new DuctCommand { Label = "Cut", Execute = () => { } };

        var result = ctx.UseCommand(original);

        Assert.Same(original, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Async command wrapping
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Async_Command_Returns_With_Execute_Set_And_ExecuteAsync_Null()
    {
        var ctx = CreateContext();
        var cmd = new DuctCommand { Label = "Save", ExecuteAsync = () => Task.CompletedTask };

        var result = ctx.UseCommand(cmd);

        Assert.NotNull(result.Execute);
        Assert.Null(result.ExecuteAsync);
    }

    [Fact]
    public void IsExecuting_Is_False_Initially()
    {
        var ctx = CreateContext();
        var cmd = new DuctCommand { Label = "Save", ExecuteAsync = () => Task.CompletedTask };

        var result = ctx.UseCommand(cmd);

        Assert.False(result.IsExecuting);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task IsExecuting_Becomes_True_During_Execution()
    {
        var tcs = new TaskCompletionSource();
        var ctx = CreateContext();
        var cmd = new DuctCommand
        {
            Label = "Save",
            ExecuteAsync = async () =>
            {
                await tcs.Task;
            }
        };

        var result = ctx.UseCommand(cmd);
        result.Execute!();

        // Give the task a moment to start
        await Task.Delay(50);

        // Re-render to observe state
        Rerender(ctx);
        var result2 = ctx.UseCommand(cmd);
        Assert.True(result2.IsExecuting);
        Assert.False(result2.IsEnabled);

        // Complete the task
        tcs.SetResult();
        await Task.Delay(50);

        // Re-render to observe completion
        Rerender(ctx);
        var result3 = ctx.UseCommand(cmd);
        Assert.False(result3.IsExecuting);
        Assert.True(result3.IsEnabled);
    }

    [Fact]
    public async Task Error_In_ExecuteAsync_Still_Resets_IsExecuting()
    {
        var ctx = CreateContext();
        var cmd = new DuctCommand
        {
            Label = "Save",
            ExecuteAsync = () => throw new InvalidOperationException("test error")
        };

        var result = ctx.UseCommand(cmd);
        result.Execute!();

        await Task.Delay(100);

        Rerender(ctx);
        var result2 = ctx.UseCommand(cmd);
        Assert.False(result2.IsExecuting);
    }

    // ════════════════════════════════════════════════════════════════
    //  Parameterized UseCommand<T>
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Sync_Parameterized_Command_Passes_Through()
    {
        var ctx = CreateContext();
        var original = new DuctCommand<string> { Label = "Delete", Execute = _ => { } };

        var result = ctx.UseCommand(original);

        Assert.Same(original, result);
    }

    [Fact]
    public async Task Parameterized_Async_Passes_Argument_Through()
    {
        string? received = null;
        var tcs = new TaskCompletionSource();
        var ctx = CreateContext();
        var cmd = new DuctCommand<string>
        {
            Label = "Delete",
            ExecuteAsync = async arg =>
            {
                received = arg;
                await tcs.Task;
            }
        };

        var result = ctx.UseCommand(cmd);
        result.Execute!("item-42");

        await Task.Delay(50);
        Assert.Equal("item-42", received);

        Rerender(ctx);
        var result2 = ctx.UseCommand(cmd);
        Assert.True(result2.IsExecuting);

        tcs.SetResult();
        await Task.Delay(50);

        Rerender(ctx);
        var result3 = ctx.UseCommand(cmd);
        Assert.False(result3.IsExecuting);
    }
}
