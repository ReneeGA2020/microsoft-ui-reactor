#pragma once

#include "element.h"
#include <string>
#include <vector>
#include <functional>
#include <initializer_list>

namespace duct {

// --- Option structs (for designated initializers) ---

struct TextFieldOptions {
    std::optional<std::string> placeholder;
    std::optional<std::string> header;
};

struct CheckBoxOptions {
    std::optional<std::string> label;
};

struct ToggleSwitchOptions {
    std::optional<std::string> on_content;
    std::optional<std::string> off_content;
};

struct RadioButtonOptions {
    std::optional<std::string> label;
    std::optional<std::string> group_name;
};

// --- Text elements ---
Element text(std::string content);
Element heading(std::string content);
Element sub_heading(std::string content);
Element caption(std::string content);

// --- Interactive ---
Element button(std::string label, std::function<void()> on_click);
Element text_field(std::string value, std::function<void(std::string)> on_changed, TextFieldOptions opts = {});
Element check_box(bool checked, std::function<void(bool)> on_changed, CheckBoxOptions opts = {});
Element toggle_switch(bool on, std::function<void(bool)> on_changed, ToggleSwitchOptions opts = {});
Element radio_button(bool checked, std::function<void(bool)> on_changed, RadioButtonOptions opts = {});
Element slider(double value, double min, double max, std::function<void(double)> on_changed = {});

// Integer overload: avoids static_cast<double>/static_cast<int> round-trips
inline Element slider(int value, int min, int max, std::function<void(int)> on_changed = {}) {
    std::function<void(double)> adapted;
    if (on_changed) {
        adapted = [on_changed = std::move(on_changed)](double v) { on_changed(static_cast<int>(v)); };
    }
    return slider(static_cast<double>(value), static_cast<double>(min), static_cast<double>(max), std::move(adapted));
}
Element progress(double value);
Element progress_indeterminate();
Element combo_box(int selected_index, std::vector<std::string> items, std::function<void(int)> on_changed);

// --- Layout: initializer_list overloads ---
Element vstack(std::initializer_list<Element> children);
Element vstack(double spacing, std::initializer_list<Element> children);
Element hstack(std::initializer_list<Element> children);
Element hstack(double spacing, std::initializer_list<Element> children);

// --- Layout: vector overloads (dynamic children) ---
Element vstack(double spacing, std::vector<Element> children);
Element hstack(double spacing, std::vector<Element> children);

// --- Grid ---
struct GridDef {
    std::string columns;
    std::string rows;
};

Element grid(GridDef def, std::initializer_list<Element> children);
Element grid(GridDef def, std::vector<Element> children);

// --- Containers ---
Element border(Element child);
Element scroll_view(Element child);

// --- Image ---
Element image(std::string uri);

// --- Empty ---
Element empty();

// --- Conditional helper ---
Element when(bool condition, std::function<Element()> builder);

// --- Component mounting ---
template<typename T>
Element component() {
    return Element(ComponentElement{ std::make_shared<T>(), std::type_index(typeid(T)) });
}

// --- Function component ---
Element func(std::function<Element(RenderContext&)> render_fn);

// --- ListView ---
struct ListViewOptions {
    std::optional<int> selected_index;
    std::function<void(int)> on_selection_changed;
};
Element list_view(std::vector<Element> items, ListViewOptions opts = {});

// --- Flyout button (content flyout) ---
Element flyout_button(std::string label, std::initializer_list<Element> flyout_children);
Element flyout_button(std::string label, std::vector<Element> flyout_children);

// --- Menu flyout button ---
struct MenuItemDef {
    std::string label;
    std::function<void()> on_click;
};
Element menu_flyout_button(std::string label, std::vector<MenuItemDef> items);

} // namespace duct
