#include "pch.h"
#include "reconciler.h"

namespace duct {

xaml::UIElement Reconciler::reconcile(
    const Element* old_el,
    const Element& new_el,
    xaml::UIElement old_control,
    std::function<void()> request_rerender)
{
    // New element is empty — unmount old
    if (std::holds_alternative<EmptyElement>(new_el.data)) {
        if (old_control) unmount(old_control);
        return nullptr;
    }

    // No old element or old was empty — mount new
    if (!old_el || std::holds_alternative<EmptyElement>(old_el->data) || !old_control) {
        return mount(new_el, request_rerender);
    }

    // Both exist — try to update
    if (can_update(*old_el, new_el)) {
#ifdef DUCT_DEBUG_LOG
        OutputDebugStringA(("RECONCILE: update in place (variant=" + std::to_string(new_el.data.index()) + ")\n").c_str());
#endif
        auto replacement = update(*old_el, new_el, old_control, request_rerender);
        if (replacement && replacement != old_control) {
            unmount(old_control);
        }
        return replacement ? replacement : old_control;
    }

    // Type changed — unmount old, mount new
#ifdef DUCT_DEBUG_LOG
    OutputDebugStringA(("RECONCILE: type changed " + std::to_string(old_el->data.index()) + " -> " + std::to_string(new_el.data.index()) + ", remount\n").c_str());
#endif
    unmount(old_control);
    return mount(new_el, request_rerender);
}

uint64_t Reconciler::get_component_id(xaml::UIElement control) {
    if (auto fe = control.try_as<xaml::FrameworkElement>()) {
        if (auto tag = fe.Tag()) {
            return winrt::unbox_value_or<uint64_t>(tag, 0);
        }
    }
    return 0;
}

void Reconciler::set_component_id(xaml::FrameworkElement control, uint64_t id) {
    control.Tag(winrt::box_value(id));
}

void Reconciler::unmount(xaml::UIElement control) {
    if (!control) return;

    // Check for component node cleanup
    uint64_t key = get_component_id(control);
    auto it = key ? component_nodes_.find(key) : component_nodes_.end();
    if (it != component_nodes_.end()) {
        auto& node = it->second;
        if (node.component) {
            node.component->cleanup();
        }
        if (node.func_context) {
            node.func_context->cleanup_all_effects();
        }
        // Recursively unmount the rendered child
        if (node.rendered_control) {
            unmount(node.rendered_control);
        }
        component_nodes_.erase(it);
    }

    // Recursively unmount children of panels
    if (auto panel = control.try_as<controls::Panel>()) {
        auto children = panel.Children();
        for (uint32_t i = 0; i < children.Size(); ++i) {
            unmount(children.GetAt(i));
        }
    }

    // Unmount border child
    if (auto border = control.try_as<controls::Border>()) {
        if (border.Child()) {
            unmount(border.Child());
        }
    }

    // Unmount scroll viewer child
    if (auto sv = control.try_as<controls::ScrollViewer>()) {
        if (auto content = sv.Content()) {
            if (auto ui = content.try_as<xaml::UIElement>()) {
                unmount(ui);
            }
        }
    }

    // Return control to pool for reuse
    pool_.release(control);
}

void Reconciler::reconcile_children(
    const std::vector<Element>& old_children,
    const std::vector<Element>& new_children,
    controls::Panel panel,
    std::function<void()> request_rerender)
{
    PanelChildCollection children(panel);
    ChildReconciler::reconcile(old_children, new_children, children, *this, request_rerender);
}

} // namespace duct
