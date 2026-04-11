using Duct.Animation;
using Duct.AppTests.Host.SelfTest;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class ScrollAnimationTests
{
    internal class ParallaxExpression(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            var builder = new ScrollAnimationBuilder().Parallax(0.5f);
            var expressions = builder.Build();

            H.Check("ScrollAnim_Parallax_Count", expressions.Length == 1);
            H.Check("ScrollAnim_Parallax_Property", expressions[0].Property == "Offset.Y");
            H.Check("ScrollAnim_Parallax_HasScroll", expressions[0].Expression.Contains("scroll.Translation.Y"));
        }
    }

    internal class FadeOutExpression(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            var builder = new ScrollAnimationBuilder().FadeOut(0, 200);
            var expressions = builder.Build();

            H.Check("ScrollAnim_FadeOut_Count", expressions.Length == 1);
            H.Check("ScrollAnim_FadeOut_Property", expressions[0].Property == "Opacity");
            H.Check("ScrollAnim_FadeOut_HasClamp", expressions[0].Expression.Contains("Clamp"));
        }
    }

    internal class FadeInExpression(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            var builder = new ScrollAnimationBuilder().FadeIn(100, 300);
            var expressions = builder.Build();

            H.Check("ScrollAnim_FadeIn_Count", expressions.Length == 1);
            H.Check("ScrollAnim_FadeIn_Property", expressions[0].Property == "Opacity");
            H.Check("ScrollAnim_FadeIn_HasClamp", expressions[0].Expression.Contains("Clamp"));
        }
    }

    internal class ScaleRangeExpression(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            var builder = new ScrollAnimationBuilder().ScaleRange(0, 400, 1.0f, 0.3f);
            var expressions = builder.Build();

            H.Check("ScrollAnim_ScaleRange_Count", expressions.Length == 1);
            H.Check("ScrollAnim_ScaleRange_Property", expressions[0].Property == "Scale");
            H.Check("ScrollAnim_ScaleRange_HasLerp", expressions[0].Expression.Contains("Lerp"));
        }
    }

    internal class CustomExpression(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            var builder = new ScrollAnimationBuilder()
                .Expression("Opacity", "Clamp(1.0 + (scroll.Translation.Y / 300), 0, 1)");
            var expressions = builder.Build();

            H.Check("ScrollAnim_Custom_Count", expressions.Length == 1);
            H.Check("ScrollAnim_Custom_Property", expressions[0].Property == "Opacity");
            H.Check("ScrollAnim_Custom_Expression", expressions[0].Expression == "Clamp(1.0 + (scroll.Translation.Y / 300), 0, 1)");
        }
    }
}
