#include "pch.h"
#include <duct/dsl.h>

namespace duct {

// --- Text elements ---
Element text(std::string content) {
    return Element(TextElement{ std::move(content) });
}
Element heading(std::string content) {
    return Element(HeadingElement{ std::move(content) });
}
Element sub_heading(std::string content) {
    return Element(SubHeadingElement{ std::move(content) });
}
Element caption(std::string content) {
    return Element(CaptionElement{ std::move(content) });
}

// --- Interactive ---
Element button(std::string label, std::function<void()> on_click) {
    return Element(ButtonElement{ std::move(label), std::move(on_click) });
}

Element text_field(std::string value, std::function<void(std::string)> on_changed, TextFieldOptions opts) {
    return Element(TextFieldElement{
        std::move(value),
        std::move(on_changed),
        std::move(opts.placeholder),
        std::move(opts.header)
    });
}

Element check_box(bool checked, std::function<void(bool)> on_changed, CheckBoxOptions opts) {
    return Element(CheckBoxElement{
        checked,
        std::move(on_changed),
        std::move(opts.label)
    });
}

Element toggle_switch(bool on, std::function<void(bool)> on_changed, ToggleSwitchOptions opts) {
    return Element(ToggleSwitchElement{
        on,
        std::move(on_changed),
        std::move(opts.on_content),
        std::move(opts.off_content)
    });
}

Element radio_button(bool checked, std::function<void(bool)> on_changed, RadioButtonOptions opts) {
    return Element(RadioButtonElement{
        checked,
        std::move(on_changed),
        std::move(opts.label),
        std::move(opts.group_name)
    });
}

Element slider(double value, double min, double max, std::function<void(double)> on_changed) {
    return Element(SliderElement{ value, min, max, std::move(on_changed) });
}

Element progress(double value) {
    return Element(ProgressElement{ value, false });
}

Element progress_indeterminate() {
    return Element(ProgressElement{ 0.0, true });
}

Element combo_box(int selected_index, std::vector<std::string> items, std::function<void(int)> on_changed) {
    return Element(ComboBoxElement{ selected_index, std::move(items), std::move(on_changed) });
}

// --- Layout: initializer_list ---
Element vstack(std::initializer_list<Element> children) {
    return Element(StackElement{ Orientation::Vertical, 0.0, std::vector<Element>(children) });
}
Element vstack(double spacing, std::initializer_list<Element> children) {
    return Element(StackElement{ Orientation::Vertical, spacing, std::vector<Element>(children) });
}
Element hstack(std::initializer_list<Element> children) {
    return Element(StackElement{ Orientation::Horizontal, 0.0, std::vector<Element>(children) });
}
Element hstack(double spacing, std::initializer_list<Element> children) {
    return Element(StackElement{ Orientation::Horizontal, spacing, std::vector<Element>(children) });
}

// --- Layout: vector ---
Element vstack(double spacing, std::vector<Element> children) {
    return Element(StackElement{ Orientation::Vertical, spacing, std::move(children) });
}
Element hstack(double spacing, std::vector<Element> children) {
    return Element(StackElement{ Orientation::Horizontal, spacing, std::move(children) });
}

// --- Grid ---
Element grid(GridDef def, std::initializer_list<Element> children) {
    return Element(GridElement{
        GridDefinition{ std::move(def.columns), std::move(def.rows) },
        std::vector<Element>(children)
    });
}
Element grid(GridDef def, std::vector<Element> children) {
    return Element(GridElement{
        GridDefinition{ std::move(def.columns), std::move(def.rows) },
        std::move(children)
    });
}

// --- Containers ---
Element border(Element child) {
    return Element(BorderElement{ BoxedElement(std::move(child)) });
}
Element scroll_view(Element child) {
    return Element(ScrollViewElement{ BoxedElement(std::move(child)) });
}

// --- Image ---
Element image(std::string uri) {
    return Element(ImageElement{ std::move(uri) });
}

// --- Empty ---
Element empty() {
    return Element(EmptyElement{});
}

// --- Conditional helper ---
Element when(bool condition, std::function<Element()> builder) {
    if (condition) return builder();
    return empty();
}

// --- Function component ---
Element func(std::function<Element(RenderContext&)> render_fn) {
    return Element(FuncElement{ std::move(render_fn) });
}

// --- ListView ---
Element list_view(std::vector<Element> items, ListViewOptions opts) {
    return Element(ListViewElement{
        std::move(items),
        opts.selected_index,
        std::move(opts.on_selection_changed)
    });
}

// --- Flyout button ---
Element flyout_button(std::string label, std::initializer_list<Element> flyout_children) {
    return Element(FlyoutButtonElement{
        std::move(label),
        std::vector<Element>(flyout_children)
    });
}
Element flyout_button(std::string label, std::vector<Element> flyout_children) {
    return Element(FlyoutButtonElement{
        std::move(label),
        std::move(flyout_children)
    });
}

// --- Menu flyout button ---
Element menu_flyout_button(std::string label, std::vector<MenuItemDef> items) {
    std::vector<MenuFlyoutItemDef> defs;
    for (auto& item : items) {
        defs.push_back(MenuFlyoutItemDef{ std::move(item.label), std::move(item.on_click) });
    }
    return Element(MenuFlyoutButtonElement{
        std::move(label),
        std::move(defs)
    });
}

} // namespace duct
