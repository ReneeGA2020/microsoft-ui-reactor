using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Localization;
using Microsoft.UI.Xaml;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Localization;

public class LocaleProviderTests
{
    [Fact]
    public void UseIntl_WithoutProvider_ReturnsDefaultAccessor()
    {
        var ctx = new RenderContext();
        var scope = new ContextScope();
        ctx.BeginRender(() => { }, scope);

        var t = ctx.UseIntl();

        Assert.NotNull(t);
        Assert.NotEmpty(t.Locale);
    }

    [Fact]
    public void UseIntl_WithContext_ReturnsProvidedAccessor()
    {
        var provider = new InMemoryResourceProvider()
            .Add("fr-FR", "Common", "Hello", "Bonjour");

        var accessor = new IntlAccessor("fr-FR", provider, new MessageCache(), "en-US");

        var scope = new ContextScope();
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = accessor });

        var ctx = new RenderContext();
        ctx.BeginRender(() => { }, scope);
        var t = ctx.UseIntl();

        Assert.Equal("fr-FR", t.Locale);
        Assert.Equal("Bonjour", t.Message(new MessageKey("Common", "Hello")));

        scope.Pop(1);
    }

    [Fact]
    public void UseIntl_ContextChange_TriggersRerender()
    {
        var provider = new InMemoryResourceProvider()
            .Add("en-US", "Common", "Hello", "Hello")
            .Add("ar-SA", "Common", "Hello", "مرحبا");

        var cache = new MessageCache();
        var enAccessor = new IntlAccessor("en-US", provider, cache, "en-US");
        var arAccessor = new IntlAccessor("ar-SA", provider, cache, "en-US");

        var scope = new ContextScope();
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = enAccessor });

        var ctx = new RenderContext();
        ctx.BeginRender(() => { }, scope);
        var t = ctx.UseIntl();
        ctx.FlushEffects();

        Assert.Equal("en-US", t.Locale);
        scope.Pop(1);

        // Change context to Arabic
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = arAccessor });
        ctx.BeginRender(() => { }, scope);
        var t2 = ctx.UseIntl();
        ctx.FlushEffects();

        Assert.Equal("ar-SA", t2.Locale);
        Assert.Equal("مرحبا", t2.Message(new MessageKey("Common", "Hello")));
        scope.Pop(1);
    }

    [Fact]
    public void UseIntl_ContextHooks_Detects_Change()
    {
        var provider = new InMemoryResourceProvider();
        var cache = new MessageCache();
        var enAccessor = new IntlAccessor("en-US", provider, cache, "en-US");
        var frAccessor = new IntlAccessor("fr-FR", provider, cache, "en-US");

        var scope = new ContextScope();
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = enAccessor });

        var ctx = new RenderContext();
        ctx.BeginRender(() => { }, scope);
        ctx.UseIntl();
        ctx.FlushEffects();
        scope.Pop(1);

        // Check that context hook tracks the change
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = frAccessor });
        var hooks = ctx.ContextHooks.ToList();
        Assert.Single(hooks);

        // The last value was enAccessor, current scope has frAccessor
        var currentValue = scope.Read(IntlContexts.Locale);
        Assert.NotEqual(hooks[0].LastValue, currentValue);
        scope.Pop(1);
    }

    [Fact]
    public void LocaleContext_NestedProviders_RestoresPrevious()
    {
        var provider = new InMemoryResourceProvider();
        var cache = new MessageCache();

        var outerAccessor = new IntlAccessor("en-US", provider, cache, "en-US");
        var innerAccessor = new IntlAccessor("fr-FR", provider, cache, "en-US");

        var scope = new ContextScope();

        // Outer provider
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = outerAccessor });

        var ctx1 = new RenderContext();
        ctx1.BeginRender(() => { }, scope);
        Assert.Equal("en-US", ctx1.UseIntl().Locale);

        // Inner provider (shadows outer)
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = innerAccessor });

        var ctx2 = new RenderContext();
        ctx2.BeginRender(() => { }, scope);
        Assert.Equal("fr-FR", ctx2.UseIntl().Locale);

        // Pop inner → outer value restored
        scope.Pop(1);

        var ctx3 = new RenderContext();
        ctx3.BeginRender(() => { }, scope);
        Assert.Equal("en-US", ctx3.UseIntl().Locale);

        scope.Pop(1);
    }

    [Fact]
    public void Integration_LocaleSwitch_DirectionFlipsAndStringsChange()
    {
        var provider = new InMemoryResourceProvider()
            .Add("en-US", "Common", "Save", "Save")
            .Add("ar-SA", "Common", "Save", "حفظ");

        var cache = new MessageCache();
        var enAccessor = new IntlAccessor("en-US", provider, cache, "en-US");
        var arAccessor = new IntlAccessor("ar-SA", provider, cache, "en-US");

        // Start with English
        Assert.Equal(FlowDirection.LeftToRight, enAccessor.Direction);
        Assert.Equal("Save", enAccessor.Message(new MessageKey("Common", "Save")));

        // Switch to Arabic
        Assert.Equal(FlowDirection.RightToLeft, arAccessor.Direction);
        Assert.Equal("حفظ", arAccessor.Message(new MessageKey("Common", "Save")));
    }

    [Fact]
    public void UseIntl_Via_Component_UseContext()
    {
        var provider = new InMemoryResourceProvider()
            .Add("de-DE", "Common", "Hello", "Hallo");

        var accessor = new IntlAccessor("de-DE", provider, new MessageCache(), "en-US");
        var scope = new ContextScope();
        scope.Push(new Dictionary<ContextBase, object?> { [IntlContexts.Locale] = accessor });

        // Simulate a component using UseIntl via the Component base class
        var comp = new IntlTestComponent();
        comp.Context.BeginRender(() => { }, scope);
        comp.Render();
        comp.Context.FlushEffects();

        Assert.Equal("de-DE", comp.LastLocale);
        Assert.Equal("Hallo", comp.LastMessage);
        scope.Pop(1);
    }

    private class IntlTestComponent : Component
    {
        public string? LastLocale;
        public string? LastMessage;

        public override Element Render()
        {
            var intl = UseIntl();
            LastLocale = intl.Locale;
            LastMessage = intl.Message(new MessageKey("Common", "Hello"));
            return new TextBlockElement(LastMessage ?? "");
        }
    }
}
