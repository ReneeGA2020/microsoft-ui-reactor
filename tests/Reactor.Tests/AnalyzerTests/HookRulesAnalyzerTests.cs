using Microsoft.UI.Reactor.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.AnalyzerTests;

/// <summary>
/// Unit tests for the REACTOR_HOOKS_001/004/005 analyzer. Tests embed minimal stub
/// types in the <c>Microsoft.UI.Reactor.Core</c> namespace so the analyzer's
/// fully-qualified-name receiver check fires without pulling in the real framework.
/// </summary>
public class HookRulesAnalyzerTests
{
    private const string Stubs = @"
namespace Microsoft.UI.Reactor.Core
{
    public class RenderContext { }

    public abstract class Component
    {
        protected internal RenderContext Context { get; } = new RenderContext();
        public abstract string Render();
        protected (int, System.Action<int>) UseState(int initial) => (0, _ => { });
        protected void UseEffect(System.Action effect, params object[] deps) { }
        protected T UseMemo<T>(System.Func<T> factory, params object[] deps) => factory();
    }
}

namespace Microsoft.UI.Reactor.Hooks
{
    using Microsoft.UI.Reactor.Core;
    public static class Extensions
    {
        public static int UseCustom(this RenderContext ctx, object[] deps) => 0;
    }
}
";

    // Stubs for REACTOR_HOOKS_006. Separate constant so adding signatures here doesn't
    // shift line numbers baked into the other rules' WithSpan assertions.
    private const string ResourceStubs = @"
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UI.Reactor.Core
{
    public class RenderContext { }

    public abstract class Component
    {
        protected internal RenderContext Context { get; } = new RenderContext();
        public abstract string Render();
        protected T UseResource<T>(System.Func<CancellationToken, Task<T>> fetcher, object[] deps) => default(T);
    }

    public static class Fakes
    {
        public static Task<int> GetUserAsync(CancellationToken ct) => Task.FromResult(0);
        public static Task<int> PostMessageAsync(CancellationToken ct) => Task.FromResult(0);
        public static Task<int> CreateOrderAsync(CancellationToken ct) => Task.FromResult(0);
        public static Task<int> DeleteItemAsync(int id, CancellationToken ct) => Task.FromResult(0);
        public static Task<int> GeneratePostalCodeAsync(CancellationToken ct) => Task.FromResult(0);
        public static Task<int> PostalLookupAsync(CancellationToken ct) => Task.FromResult(0);
    }
}
";

    private static DiagnosticResult Diagnostic(string id) =>
        CSharpAnalyzerVerifier<HookRulesAnalyzer, DefaultVerifier>.Diagnostic(id);

    // ── REACTOR_HOOKS_001 — conditional hook ──────────────────────────

    [Fact]
    public async Task Hook_Inside_If_Flags_Conditional()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            var (count, setCount) = UseState(0);
        }
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.ConditionalHookId)
            .WithSpan(31, 37, 31, 48)
            .WithArguments("UseState", "if");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_Inside_ForEach_Flags_Conditional()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        foreach (var i in new[] { 1, 2, 3 })
        {
            var (count, setCount) = UseState(0);
        }
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.ConditionalHookId)
            .WithSpan(31, 37, 31, 48)
            .WithArguments("UseState", "foreach loop");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_Inside_Lambda_Flags_Conditional()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        System.Action a = () => UseEffect(() => { }, new object[0]);
        return """";
    }
}";
        // The lambda UseEffect call triggers both the conditional rule (it's inside a
        // nested lambda) and the unstable-deps rule (the `new object[0]` deps literal).
        var conditional = Diagnostic(HookRulesAnalyzer.ConditionalHookId)
            .WithSpan(29, 33, 29, 68)
            .WithArguments("UseEffect", "nested lambda/local function");
        var unstableDeps = Diagnostic(HookRulesAnalyzer.UnstableDepsId)
            .WithSpan(29, 54, 29, 67)
            .WithArguments("UseEffect", "array");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { conditional, unstableDeps },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_At_Top_Of_Render_No_Diagnostic()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        var (count, setCount) = UseState(0);
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    // ── REACTOR_HOOKS_004 — freshly-allocated deps ────────────────────

    [Fact]
    public async Task UseEffect_With_New_List_Dep_Flags()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        UseEffect(() => { }, new System.Collections.Generic.List<int>());
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.UnstableDepsId)
            .WithSpan(29, 30, 29, 72)
            .WithArguments("UseEffect", "object");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseEffect_With_Lambda_Dep_Flags()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        UseEffect(() => { }, (System.Func<int>)(() => 1));
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.UnstableDepsId)
            .WithSpan(29, 30, 29, 57)
            .WithArguments("UseEffect", "lambda");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseEffect_With_Scalar_Dep_No_Diagnostic()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        int userId = 42;
        UseEffect(() => { }, userId);
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    // ── REACTOR_HOOKS_005 — hook outside Render/custom hook ────────────

    [Fact]
    public async Task Hook_In_Event_Handler_Flags_Outside_Render()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    void HandleClick()
    {
        var (count, setCount) = UseState(0);
    }
    public override string Render() => """";
}";
        var expected = Diagnostic(HookRulesAnalyzer.HookOutsideRenderId)
            .WithSpan(29, 33, 29, 44)
            .WithArguments("UseState");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_In_Custom_Use_Method_No_Diagnostic()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    int UseCustom()
    {
        var (count, setCount) = UseState(0);
        return count;
    }
    public override string Render()
    {
        var c = UseCustom();
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    // ── Non-hook lookalike ────────────────────────────────────────────

    // ── REACTOR_HOOKS_006 — non-idempotent fetcher ────────────────────

    [Fact]
    public async Task UseResource_With_Post_Method_Reference_Flags()
    {
        var test = ResourceStubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        var x = UseResource<int>(Microsoft.UI.Reactor.Core.Fakes.PostMessageAsync, new object[] { 1 });
        return """";
    }
}";
        // WithSpan points at the method-name identifier in the member-access expression.
        var expected = Diagnostic(HookRulesAnalyzer.NonIdempotentFetcherId)
            .WithSpan(31, 66, 31, 82)
            .WithArguments("UseResource", "PostMessage");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseResource_With_Lambda_Calling_DeleteAsync_Flags()
    {
        var test = ResourceStubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        var x = UseResource<int>(ct => Microsoft.UI.Reactor.Core.Fakes.DeleteItemAsync(1, ct), new object[] { 1 });
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.NonIdempotentFetcherId)
            .WithSpan(31, 72, 31, 87)
            .WithArguments("UseResource", "DeleteItem");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseResource_With_Lambda_Calling_GenerateAsync_Flags()
    {
        var test = ResourceStubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        var x = UseResource<int>(ct => Microsoft.UI.Reactor.Core.Fakes.GeneratePostalCodeAsync(ct), new object[] { 1 });
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.NonIdempotentFetcherId)
            .WithSpan(31, 72, 31, 95)
            .WithArguments("UseResource", "GeneratePostalCode");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseResource_With_Get_Method_No_Diagnostic()
    {
        var test = ResourceStubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        var x = UseResource<int>(ct => Microsoft.UI.Reactor.Core.Fakes.GetUserAsync(ct), new object[] { 1 });
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseResource_With_PostalLookup_Does_Not_Match_Post_Word_Boundary()
    {
        // Word-boundary guard: `PostalLookup` shares the `Post` prefix but the next
        // letter is lower-case, so it's not treated as a `Post<Word>` match.
        var test = ResourceStubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        var x = UseResource<int>(ct => Microsoft.UI.Reactor.Core.Fakes.PostalLookupAsync(ct), new object[] { 1 });
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    // ── Non-Reactor lookalike ────────────────────────────────────────

    [Fact]
    public async Task Non_Reactor_Use_Method_Not_Flagged()
    {
        // `UseAuthentication`-style helpers outside Reactor's type hierarchy should be
        // left alone.
        var test = @"
class Builder
{
    public Builder UseAuthentication() => this;
}
class Program
{
    static void Main()
    {
        // Called outside Render, inside an if — but receiver isn't a Reactor type.
        var b = new Builder();
        if (true) { b.UseAuthentication(); }
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }
}
