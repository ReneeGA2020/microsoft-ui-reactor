# Microsoft.UI.Reactor — A New Way to Build WinUI Apps

**Status:** Experimental  
**April 2026**

---

## What is Reactor?

Reactor is a declarative, component-based C# framework for building WinUI desktop applications. Inspired by React, SwiftUI, and Jetpack Compose, it lets developers describe UI as pure C# functions — no XAML, no data binding, no ViewModels. A virtual element tree and reconciler handle the rest: diffing old vs. new descriptions and patching only what changed on real WinUI controls.

```csharp
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.Run<MyApp>("Hello Reactor");

class MyApp : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        return VStack(
            Heading($"Count: {count}"),
            HStack(8,
                Button("-", () => setCount(count - 1)),
                Button("+", () => setCount(count + 1))
            )
        );
    }
}
```

Reactor is **not** a new UI platform. It is a new way to create WinUI content. Every control is a real WinUI control — Button, TextBox, NavigationView, TreeView — just described differently. Apps built with Reactor can freely interop with XAML, existing WinUI controls, and MVVM code.

---

## Why we built it

WinUI is a powerful native UI platform, but its XAML/MVVM programming model hasn't kept pace with the developer experience innovations in React, SwiftUI, and Compose. Developers working in those ecosystems expect co-located state, declarative composition, and type-safe UI construction. Reactor brings those patterns to WinUI without abandoning the underlying platform. Our goal is to make WinUI more accessible to a broader range of developers — not to create a walled garden. 


To prove out this approach, we have addressed some of the current gaps in the WinUI story by building out controls and services currently lacking from WinUI. Some of the controls and features we built within Reactor will likely migrate into WinUI directly; others may continue to be delivered through Reactor. Either way, the WinUI platform is the foundation.

---

## What's included

Reactor spans the core framework and a set of higher-level features. Each area below is labeled by its current maturity.

| Area | What it does | Maturity |
|---|---|---|
| **Core reconciler** | Virtual element tree, keyed diffing, element pooling, render coalescing, skip-unchanged optimization | Preview |
| **DSL & elements** | Factory methods covering WinUI controls, fluent modifier chains, attached properties | Preview |
| **Hooks & state** | UseState, UseReducer, UseEffect, UseMemo, UseRef, UseObservable, UseCollection | Preview |
| **Flex layout** | C# port of facebook/yoga with FlexPanel, 590 ported test fixtures | Preview |
| **Commanding** | Command records bundling label, icon, shortcut, and action; 16 standard commands; focus-scoped accelerators | Preview |
| **Charting (D3)** | Full D3 algorithm port plus declarative chart DSL — line, bar, area, pie, tree, force-directed graphs | Preview |
| **Markdown** | Native md4c parser with Markdown() element builder | Preview |
| **Navigation** | Type-safe declarative routing, GPU composition transitions, lifecycle guards, back-stack serialization | Preview |
| **Accessibility** | AutomationProperties modifiers, WCAG 2.1 AA target, compile & runtime validation | Preview |
| **WinForms Interop** | Simple hosting of WinUI content | Draft |
| **Theming & styling** | ThemeRef tokens, dark/light/high-contrast, style caching, lightweight per-control resource overrides, Roslyn analyzers | Draft |
| **Animation** | Compositor-layer transitions, keyframes, stagger, scroll-linked animation, connected animations | Draft |
| **Localization** | ICU message format, source generator, CLI tooling (extract, translate, validate), RTL/BiDi | Draft |
| **Lists & virtualization** | Virtualized ListView, GridView, ItemsRepeater, LazyStack with recycling | Draft |
| **Data system** | DataGrid, PropertyGrid, FormField, FieldDescriptor metadata model, async validation, inline editing | Early |
| **Preview / hot reload** | MetadataUpdateHandler hot reload, CLI --preview flag, VS Code live preview | Early |
| **Monaco integration** | WebView2-based Monaco code editor element | Early |

---

## What's in it for you

### Fundamentals — for everyone

The core framework has been through 13 days of continuous reconciler iteration, a full competitive review against React, SwiftUI, and Compose, and a 275-finding code review. It targets the basics that every developer cares about:

- **Performance:** Element pooling, render coalescing, skip-unchanged optimization, native interop bypass of CsWinRT overhead. Deliver all the functionality while maintaining a high bar of perforamnce
- **Stability:** Implemented as a system component with high bar on reliability, logging, stress testing, etc.
- **Developer experience:** Full IntelliSense, refactoring, and compile-time type safety. No XAML string-typing, no binding errors at runtime.
- **Localization** with ICU message format, pluralization, CLI extraction, and AI-assisted translation.
- **Accessibility** full exposure of all WinUI built in accessibility in a simple API, inline development time linting, and robust runtime validation

### Frontier developers — new programming model

If you're excited about declarative UI and functional patterns:

- **Functional MVU model** Meet modern developer and AI agent expectations with full use of C# expressions for UI creation.
- **Hooks-based state** co-located with render logic — no ViewModel boilerplate, no binding expressions.
- **Immutable element trees** with automatic reconciliation — describe what the UI *should* look like, not how to mutate it.
- **Flex layout** via a faithful yoga port — familiar to anyone who has used CSS flexbox.
- **Animation** driven by compositor-layer features — transitions, keyframes, and connected animations described declaratively.

### Enterprise developers — data and productivity

If you're building line-of-business applications:

- **DataGrid** with headless state management, column DSL, sort, selection, keyboard navigation, inline editing, column resize, and async validation.
- **PropertyGrid** with metadata-driven type-to-editor registry, recursive decomposition, and INPC integration.
- **Forms & validation** pipeline with FormField, ValidationRule, and auto-validation.
- **Charting** Composable services for creating a wide variety of charts, leveraging industry standard D3.
- **Commanding** that bundles label, icon, keyboard shortcut, and action into a single definition surfaced across menus, toolbars, and context menus.
- **WinForms Migration** Simple tools to incrementally adopt WinUI in your existing application.


---

## How we're releasing it

Reactor is being released on GitHub as an **experimental** project. This label will remain for 3–6 months as we iterate on the design in the open.

We are not launching with a big public announcement. Instead, we're starting by working directly with MVPs and trusted community members to gather feedback, pressure-test the API surface, and refine the programming model before broadening adoption. We want the design to be shaped by real developer experience, not just our internal usage.

Everything in the repository — framework code, specs, sample apps, test suites — is available for anyone to read, build, and experiment with. Contributions and feedback are welcome from day one.

This is an **experiment** and you should expect that every line of code will change in this project, we may completely change the DSL syntax as we work with the C# team on language design, we may add or remove controls, we may change approach on the layering. This is your chance to get in while the sausage is getting made.

---

## Learn more

- [Getting Started](Reactor/Docs/GettingStarted.md)
- [Architecture](Reactor/Docs/Architecture.md)
- [Design Specs](../specs/) — numbered specs covering theming, navigation, animation, data, accessibility, and more
- [WinUI Integration Proposals](../specs/proposals/winui3-integration.md) — 25 proposals for deeper platform integration
