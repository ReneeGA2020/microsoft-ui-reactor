#pragma once

#include <duct/element.h>
#include <duct/component.h>
#include <duct/hooks.h>
#include "reconciler.h"

#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Dispatching.h>

namespace duct {

class DuctHost {
public:
    explicit DuctHost(winrt::Microsoft::UI::Xaml::Window window);

    // Mount a class component as root
    void mount(std::shared_ptr<Component> component);

    // Mount a function component as root
    void mount(std::function<Element(RenderContext&)> render_func);

    // Request a re-render (debounced via DispatcherQueue)
    void request_render();

    // Access the reconciler
    Reconciler& reconciler() { return reconciler_; }

private:
    void render();
    void render_loop();
    void show_error(winrt::hstring message);

    winrt::Microsoft::UI::Xaml::Window window_;
    winrt::Microsoft::UI::Dispatching::DispatcherQueue dispatcher_{ nullptr };

    Reconciler reconciler_;

    // Root component (class-based)
    std::shared_ptr<Component> root_component_;

    // Root function component
    std::function<Element(RenderContext&)> root_func_;
    std::unique_ptr<RenderContext> root_func_context_;

    // Current tree and control
    Element current_tree_;
    xaml::UIElement current_control_{ nullptr };

    // Previous tree — kept alive for tag-based event dispatch
    Element previous_tree_;

    bool needs_rerender_ = false;
    bool render_scheduled_ = false;
};

} // namespace duct
