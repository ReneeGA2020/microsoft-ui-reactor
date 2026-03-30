#pragma once

#include <duct/element.h>
#include <duct/component.h>
#include <duct/hooks.h>
#include "child_reconciler.h"
#include "element_pool.h"
#include <unordered_map>
#include <functional>

// WinRT forward declarations
#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>

namespace duct {

namespace xaml = winrt::Microsoft::UI::Xaml;
namespace controls = winrt::Microsoft::UI::Xaml::Controls;

// Tracks a mounted component or function component
struct ComponentNode {
    std::shared_ptr<Component> component;           // For ComponentElement
    std::unique_ptr<RenderContext> func_context;     // For FuncElement
    Element rendered_element;                         // Last rendered child tree
    xaml::UIElement rendered_control{ nullptr };       // The child control inside the Border wrapper
};

class Reconciler {
public:
    Reconciler() = default;

    // Main entry point
    xaml::UIElement reconcile(
        const Element* old_el,
        const Element& new_el,
        xaml::UIElement old_control,
        std::function<void()> request_rerender);

    // Mount: create WinUI control from element
    xaml::UIElement mount(
        const Element& el,
        std::function<void()> request_rerender);

    // Update: patch existing control; returns replacement if type changed, nullptr otherwise
    xaml::UIElement update(
        const Element& old_el,
        const Element& new_el,
        xaml::UIElement control,
        std::function<void()> request_rerender);

    // Unmount: cleanup component state
    void unmount(xaml::UIElement control);

    // Can we update in-place?
    static bool can_update(const Element& old_el, const Element& new_el) {
        if (old_el.data.index() != new_el.data.index()) return false;
        // ComponentElements must match on concrete component type
        if (auto* old_comp = std::get_if<ComponentElement>(&old_el.data)) {
            auto* new_comp = std::get_if<ComponentElement>(&new_el.data);
            if (old_comp->component_type != new_comp->component_type) return false;
        }
        return true;
    }

    // Child reconciliation
    void reconcile_children(
        const std::vector<Element>& old_children,
        const std::vector<Element>& new_children,
        controls::Panel panel,
        std::function<void()> request_rerender);

    // Access the element pool
    ElementPool& pool() { return pool_; }

private:
    // Component tracking — keyed by unique ID stored in Border wrapper's Tag
    std::unordered_map<uint64_t, ComponentNode> component_nodes_;
    uint64_t next_component_id_ = 1;

    // Helper: get component ID from a control's Tag
    static uint64_t get_component_id(xaml::UIElement control);
    // Helper: set component ID on a control's Tag
    void set_component_id(xaml::FrameworkElement control, uint64_t id);

    // Element pool for control recycling
    ElementPool pool_;

    // Tag-based event dispatch helpers
    static void set_element_tag(xaml::FrameworkElement control, const Element& el);

    // Apply modifiers to a control
    static void apply_modifiers(xaml::UIElement control,
                                const std::shared_ptr<ElementModifiers>& mods);

    // Apply modifier diff (only changed properties)
    static void apply_modifiers_diff(xaml::UIElement control,
                                     const std::shared_ptr<ElementModifiers>& old_mods,
                                     const std::shared_ptr<ElementModifiers>& new_mods);
};

} // namespace duct
