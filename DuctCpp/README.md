# DuctCpp

A C++20 declarative UI framework for WinUI 3, implementing a React-style virtual DOM with hooks and reconciliation. C++ port of the C# Duct framework.

## Build Instructions

### Prerequisites

- Visual Studio 2022 (17.8+) with C++ Desktop Development workload
- Windows App SDK 1.5+ (installed via NuGet)
- Windows 11 SDK (10.0.22621.0+)

### Building

1. Open `DuctCpp.sln` in Visual Studio
2. Select **x64** / **Debug** or **Release**
3. Build the solution (Ctrl+Shift+B)

**Projects in the solution:**

| Project | Type | Purpose |
|---------|------|---------|
| `DuctCpp` | Static library | Core framework (element model, hooks, reconciler) |
| `DuctCpp.TestApp` | WinUI 3 exe | 11-tab demo app exercising all framework features |
| `DuctCpp.StressPerf` | WinUI 3 exe | Benchmark app (4,900-cell stock ticker grid) |
| `DuctCpp.Tests` | Console exe | Unit tests (element model, hooks, LIS algorithm) |

### Running

- **TestApp**: Set `DuctCpp.TestApp` as startup project, F5
- **StressPerf**: Run from command line with `--headless --percent 50 --duration 10`
- **Tests**: Run `DuctCpp.Tests.exe` from the build output directory

## Architecture

```
include/duct/
  duct.h          Single convenience header (includes all below)
  element.h       Element types as std::variant + fluent modifiers
  modifiers.h     ElementModifiers struct (margin, padding, alignment, etc.)
  dsl.h           Factory functions: text(), button(), vstack(), etc.
  hooks.h         RenderContext with use_state, use_reducer, use_effect, use_memo, use_ref
  component.h     Component base class with virtual render()
  app.h           duct::run<T>() entry point

src/
  reconciler.cpp          Core reconcile/mount/update dispatch
  reconciler_mount.cpp    Mount: Element -> WinUI control creation
  reconciler_update.cpp   Update: diff old/new elements, patch WinUI controls
  child_reconciler.cpp    Keyed + positional child list reconciliation (LIS)
  element_pool.cpp        Control recycling pool (TextBlock, StackPanel, etc.)
  host.cpp                DuctHost: render loop, DispatcherQueue integration
  app.cpp                 Application bootstrap, IXamlMetadataProvider
  element.cpp             Element/BoxedElement implementation
  dsl.cpp                 DSL factory function implementations
```

### Data Flow

```
Component::render()          Pure C++ - builds Element tree (std::variant)
       |
       v
Reconciler::reconcile()      Diffs old vs new Element trees
       |
       v
mount() / update()           Creates or patches WinUI 3 controls via C++/WinRT
       |
       v
XAML layout engine           Measure/Arrange/Render (handled by WinUI)
```

### Key Design Decisions

- **`std::variant` element model** — Elements are stack-allocated value types, not polymorphic class hierarchies. Zero heap allocation for element construction.
- **Copy-on-write modifiers** — `shared_ptr<ElementModifiers>` allows cheap element copies when modifiers are unchanged.
- **Tag-based event dispatch** — WinUI control `Tag` property stores a pointer to the current Element, enabling callbacks without closures stored on the WinRT side.
- **Element pool** — Recycled WinUI controls avoid repeated COM object creation.

## Usage Example

```cpp
#include <duct/duct.h>
using namespace duct;

class MyApp : public Component {
public:
    Element render() override {
        auto [count, set_count] = use_state(0);

        return vstack(12, {
            heading("Hello DuctCpp!"),
            text(std::format("Count: {}", count)).font_size(24),
            hstack(8, {
                button("-", [=] { set_count(count - 1); }),
                button("+", [=] { set_count(count + 1); })
            })
        });
    }
};

int WINAPI wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int) {
    duct::run<MyApp>(L"My App", 800, 600);
    return 0;
}
```

## How to Add a New Control

Adding a new WinUI control type requires changes in 5 files:

### 1. Define the element type (`include/duct/element.h`)

```cpp
struct MyControlElement {
    std::string value;
    std::function<void(std::string)> on_changed;
};
```

Add it to the `ElementData` variant.

### 2. Add a factory function (`include/duct/dsl.h` + `src/dsl.cpp`)

```cpp
// dsl.h
Element my_control(std::string value, std::function<void(std::string)> on_changed);

// dsl.cpp
Element my_control(std::string value, std::function<void(std::string)> on_changed) {
    return Element(MyControlElement{ std::move(value), std::move(on_changed) });
}
```

### 3. Implement mount (`src/reconciler_mount.cpp`)

Add a case in the `mount` visitor:

```cpp
[&](const MyControlElement& el) -> UIElement {
    auto control = pool_.acquire<Controls::MyControl>();
    control.Value(to_hstring(el.value));
    // Wire events using tag-based dispatch
    set_tag(control, current_element);
    control.ValueChanged([](auto sender, auto) {
        auto& el = get_tag<MyControlElement>(sender);
        if (el.on_changed) el.on_changed(/* new value */);
    });
    return control;
}
```

### 4. Implement update (`src/reconciler_update.cpp`)

Add a case in the `update` visitor to diff old vs new and patch:

```cpp
[&](const MyControlElement& old_el, const MyControlElement& new_el) {
    auto control = old_control.as<Controls::MyControl>();
    if (old_el.value != new_el.value)
        control.Value(to_hstring(new_el.value));
    set_tag(control, current_element); // Update tag for new callbacks
}
```

### 5. Add pool support (`src/element_pool.cpp`)

Add acquire/release specialization for the new control type with property reset logic.
