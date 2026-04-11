using Duct.Animation;
using Duct.AppTests.Host.SelfTest;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class CurveTests
{
    internal class RecordEquality(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Verify record equality for Curve subtypes
            var spring1 = Curve.Spring(0.8f, 0.05f);
            var spring2 = Curve.Spring(0.8f, 0.05f);
            H.Check("Curve_SpringEquality", spring1 == spring2);

            var ease1 = Curve.Ease(300, Easing.Decelerate);
            var ease2 = Curve.Ease(300, Easing.Decelerate);
            H.Check("Curve_EaseEquality", ease1 == ease2);

            var linear1 = Curve.Linear(200);
            var linear2 = Curve.Linear(200);
            H.Check("Curve_LinearEquality", linear1 == linear2);

            // Verify different curves are not equal
            H.Check("Curve_SpringVsEaseNotEqual", spring1 != ease1);
        }
    }

    internal class PresetValues(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Verify Easing presets have expected values
            H.Check("Easing_Linear", Easing.Linear == new Easing(0f, 0f, 1f, 1f));
            H.Check("Easing_EaseIn", Easing.EaseIn == new Easing(0.42f, 0f, 1f, 1f));
            H.Check("Easing_EaseOut", Easing.EaseOut == new Easing(0f, 0f, 0.58f, 1f));
            H.Check("Easing_EaseInOut", Easing.EaseInOut == new Easing(0.42f, 0f, 0.58f, 1f));
            H.Check("Easing_Standard", Easing.Standard == new Easing(0.8f, 0f, 0.2f, 1f));
        }
    }

    internal class FactoryMethods(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Verify factory methods produce correct types
            H.Check("Curve_SpringFactory", Curve.Spring() is SpringCurve);
            H.Check("Curve_EaseFactory", Curve.Ease(300) is EaseCurve);
            H.Check("Curve_LinearFactory", Curve.Linear(200) is LinearCurve);

            // Verify CubicBezier factory
            var custom = Easing.CubicBezier(0.1f, 0.2f, 0.3f, 0.4f);
            H.Check("Easing_CubicBezier", custom.X1 == 0.1f && custom.Y1 == 0.2f && custom.X2 == 0.3f && custom.Y2 == 0.4f);

            // Verify spring parameters
            var spring = (SpringCurve)Curve.Spring(0.7f, 0.1f);
            H.Check("Curve_SpringParams", spring.DampingRatio == 0.7f && spring.Period == 0.1f);
        }
    }
}
