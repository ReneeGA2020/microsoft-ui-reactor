#pragma once

#include "component.h"
#include <string>
#include <functional>
#include <memory>

// Forward declare WinRT types used in the implementation
// (actual includes happen in the .cpp)

namespace duct {

class RenderContext;

// Run with a class component
template<typename TComponent>
void run(const std::wstring& title, int width = 1200, int height = 800, bool full_screen = false);

// Run with a function component
void run(const std::wstring& title,
         std::function<Element(RenderContext&)> render_func,
         int width = 1200, int height = 800, bool full_screen = false);

// Internal: the actual app runner (implemented in app.cpp)
void run_impl(const std::wstring& title,
              std::shared_ptr<Component> component,
              std::function<Element(RenderContext&)> render_func,
              int width, int height, bool full_screen);

template<typename TComponent>
void run(const std::wstring& title, int width, int height, bool full_screen) {
    run_impl(title, std::make_shared<TComponent>(), nullptr, width, height, full_screen);
}

} // namespace duct
