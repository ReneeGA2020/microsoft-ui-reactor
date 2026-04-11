using Duct.Core;
using Duct.Validation;
using Xunit;

namespace Duct.Tests;

public class UseValidationContextTests
{
    // ════════════════════════════════════════════════════════════════
    //  DuctContext definition
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ValidationContexts_Current_Has_Null_Default()
    {
        Assert.Null(ValidationContexts.Current.DefaultValue);
    }

    [Fact]
    public void ValidationContexts_Current_Is_Singleton()
    {
        Assert.Same(ValidationContexts.Current, ValidationContexts.Current);
    }

    // ════════════════════════════════════════════════════════════════
    //  Hook behavior via RenderContext simulation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void UseValidationContext_Returns_Same_Instance_Across_Renders()
    {
        var renderCtx = new RenderContext();
        // Simulate two render passes
        renderCtx.BeginRender(() => { });
        var ctx1 = renderCtx.UseValidationContext();

        renderCtx.BeginRender(() => { });
        var ctx2 = renderCtx.UseValidationContext();

        Assert.Same(ctx1, ctx2);
    }

    [Fact]
    public void UseValidationContext_Creates_NonNull_Context()
    {
        var renderCtx = new RenderContext();
        renderCtx.BeginRender(() => { });
        var ctx = renderCtx.UseValidationContext();

        Assert.NotNull(ctx);
        Assert.IsType<ValidationContext>(ctx);
    }

    [Fact]
    public void UseValidationContext_Returns_Parent_When_Provided_Via_ContextScope()
    {
        var parentCtx = new ValidationContext();
        parentCtx.Add("test", "msg");

        var scope = new ContextScope();
        scope.Push(new Dictionary<DuctContextBase, object?>
        {
            [ValidationContexts.Current] = parentCtx
        });

        var renderCtx = new RenderContext();
        renderCtx.BeginRender(() => { }, scope);
        var result = renderCtx.UseValidationContext();

        Assert.Same(parentCtx, result);
    }

    [Fact]
    public void UseValidationContext_Creates_Local_When_No_Parent()
    {
        var renderCtx = new RenderContext();
        renderCtx.BeginRender(() => { });
        var result = renderCtx.UseValidationContext();

        Assert.NotNull(result);
        Assert.True(result.IsValid()); // fresh context is valid
    }

    // ════════════════════════════════════════════════════════════════
    //  Child context
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void UseChildValidationContext_Creates_Independent_Child()
    {
        var renderCtx = new RenderContext();
        renderCtx.BeginRender(() => { });
        var (child, parent) = renderCtx.UseChildValidationContext();

        Assert.NotNull(child);
        Assert.Null(parent); // no parent provided
    }

    [Fact]
    public void UseChildValidationContext_Returns_Parent_When_Provided()
    {
        var parentCtx = new ValidationContext();

        var scope = new ContextScope();
        scope.Push(new Dictionary<DuctContextBase, object?>
        {
            [ValidationContexts.Current] = parentCtx
        });

        var renderCtx = new RenderContext();
        renderCtx.BeginRender(() => { }, scope);
        var (child, parent) = renderCtx.UseChildValidationContext();

        Assert.NotNull(child);
        Assert.Same(parentCtx, parent);
        Assert.NotSame(child, parent);
    }

    [Fact]
    public void UseChildValidationContext_Child_Is_Stable_Across_Renders()
    {
        var renderCtx = new RenderContext();

        renderCtx.BeginRender(() => { });
        var (child1, _) = renderCtx.UseChildValidationContext();

        renderCtx.BeginRender(() => { });
        var (child2, _) = renderCtx.UseChildValidationContext();

        Assert.Same(child1, child2);
    }
}
