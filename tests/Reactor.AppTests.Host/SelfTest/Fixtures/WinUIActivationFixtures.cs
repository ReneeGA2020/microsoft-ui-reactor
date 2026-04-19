using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Elements;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Tests that require WinUI activation context (FontFamily, SolidColorBrush, etc.).
/// Migrated from unit tests that were skipped because they need a WinUI window.
/// </summary>
internal static class WinUIActivationFixtures
{
    // ════════════════════════════════════════════════════════════════
    //  FontFamily modifier tests (from DeclarativeModifierTests)
    // ════════════════════════════════════════════════════════════════

    internal class FontFamilyStringOnButton(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var el = Button("Go").FontFamily("Segoe Fluent Icons");
            H.Check("FontFamilyStr_IsButtonElement", el is ButtonElement);
            H.Check("FontFamilyStr_ModifiersNotNull", el.Modifiers is not null);
            H.Check("FontFamilyStr_FamilyNotNull", el.Modifiers!.FontFamily is not null);
            H.Check("FontFamilyStr_SourceCorrect", el.Modifiers.FontFamily!.Source == "Segoe Fluent Icons");
            return Task.CompletedTask;
        }
    }

    internal class FontFamilyInstanceOnBorder(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var family = new FontFamily("Consolas");
            var el = Border(TextBlock("x")).FontFamily(family);
            H.Check("FontFamilyInst_IsBorderElement", el is BorderElement);
            H.Check("FontFamilyInst_SameInstance", ReferenceEquals(family, el.Modifiers!.FontFamily));
            return Task.CompletedTask;
        }
    }

    internal class FontFamilyMergeOverwrites(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var el = Button("Go").FontFamily("Arial").FontFamily("Consolas");
            H.Check("FontFamilyMerge_SourceIsConsolas", el.Modifiers!.FontFamily!.Source == "Consolas");
            return Task.CompletedTask;
        }
    }

    internal class TypographyModifiersMerge(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var family = new FontFamily("Arial");
            var a = new ElementModifiers { FontFamily = family, FontSize = 12 };
            var b = new ElementModifiers { FontSize = 16 };
            var merged = a.Merge(b);

            H.Check("TypoMerge_FamilyPreserved", ReferenceEquals(family, merged.FontFamily));
            H.Check("TypoMerge_SizeOverridden", merged.FontSize == 16.0);
            return Task.CompletedTask;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Brush tests (from ElementTests)
    // ════════════════════════════════════════════════════════════════

    internal class BorderFluentBrush(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            // .Background() and .WithBorder() store values in Modifiers.
            // Verify the element-level modifiers are set correctly.
            var el = VStack(TextBlock("x")).Background("#ff0000").WithBorder("blue", 2);
            H.Check("BorderBrush_BgNotNull", el.Modifiers?.Background is not null);
            H.Check("BorderBrush_BorderBrushNotNull", el.Modifiers?.BorderBrush is not null);
            H.Check("BorderBrush_Thickness", el.Modifiers?.BorderThickness?.Left == 2);

            var host = H.CreateHost();
            host.Mount(ctx => el);
            await Harness.Render();
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Lightweight styling / ResourceBuilder tests (from LightweightStylingTests)
    // ════════════════════════════════════════════════════════════════

    internal class ResourceBuilderStringBrush(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            // Build() is internal — test through the public .Resources() extension
            var el = Button("Go").Resources(r => r.Set("ButtonBackground", "#FF0000"));
            H.Check("ResBrush_OverridesNotNull", el.ResourceOverrides is not null);
            H.Check("ResBrush_SingleLiteral", el.ResourceOverrides!.Literals.Count == 1);
            H.Check("ResBrush_HasKey", el.ResourceOverrides.Literals.ContainsKey("ButtonBackground"));
            H.Check("ResBrush_NoThemeRefs", el.ResourceOverrides.ThemeRefs.Count == 0);
            return Task.CompletedTask;
        }
    }

    internal class ResourceBuilderFluentChaining(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            // Build() is internal — test through the public .Resources() extension
            var el = Button("Go").Resources(r => r
                .Set("ButtonBackground", "#0078D4")
                .Set("ButtonBackgroundPointerOver", "#106EBE")
                .Set("ButtonForeground", Theme.PrimaryText));
            H.Check("ResChain_TwoLiterals", el.ResourceOverrides!.Literals.Count == 2);
            H.Check("ResChain_OneThemeRef", el.ResourceOverrides.ThemeRefs.Count == 1);
            return Task.CompletedTask;
        }
    }

    internal class ResourceExtensionSetsOverrides(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var el = Button("Go").Resources(r => r
                .Set("ButtonBackground", "#0078D4")
                .Set("ButtonBackgroundPointerOver", "#106EBE"));

            H.Check("ResExt_OverridesNotNull", el.ResourceOverrides is not null);
            H.Check("ResExt_TwoLiterals", el.ResourceOverrides!.Literals.Count == 2);
            return Task.CompletedTask;
        }
    }

    internal class ResourceExtensionPreservesProps(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var el = Button("Go", () => { })
                .Margin(10)
                .Resources(r => r.Set("ButtonBackground", "#FF0000"));

            H.Check("ResPres_Label", el.Label == "Go");
            H.Check("ResPres_OnClick", el.OnClick is not null);
            H.Check("ResPres_Margin", el.Modifiers!.Margin == new Thickness(10));
            H.Check("ResPres_Overrides", el.ResourceOverrides is not null);
            return Task.CompletedTask;
        }
    }

    internal class ResourceExtensionMixedLiteralAndThemeRef(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var el = Button("Go").Resources(r => r
                .Set("ButtonBackground", "#0078D4")
                .Set("ButtonForeground", Theme.PrimaryText)
                .Set("ControlCornerRadius", new CornerRadius(4)));

            H.Check("ResMixed_TwoLiterals", el.ResourceOverrides!.Literals.Count == 2);
            H.Check("ResMixed_OneThemeRef", el.ResourceOverrides.ThemeRefs.Count == 1);
            return Task.CompletedTask;
        }
    }
}
