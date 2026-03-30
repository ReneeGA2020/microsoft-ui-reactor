#pragma once

#include "modifiers.h"
#include <string>
#include <vector>
#include <variant>
#include <optional>
#include <memory>
#include <functional>
#include <typeindex>

namespace duct {

// Forward declarations
struct Element;
class RenderContext;
class Component;

// Heap-allocated element for recursive containment (Border, ScrollView)
struct BoxedElement {
    std::unique_ptr<Element> ptr;

    BoxedElement();
    BoxedElement(Element e);
    BoxedElement(const BoxedElement& o);
    BoxedElement(BoxedElement&&) noexcept = default;
    BoxedElement& operator=(const BoxedElement& o);
    BoxedElement& operator=(BoxedElement&&) noexcept = default;

    const Element& operator*() const { return *ptr; }
    Element& operator*() { return *ptr; }
    const Element* operator->() const { return ptr.get(); }
    Element* operator->() { return ptr.get(); }
};

// --- Element type structs ---

struct EmptyElement {};

struct TextElement {
    std::string content;
    std::optional<double> font_size;
    std::optional<FontWeight> font_weight;
};

struct HeadingElement {
    std::string content;
};

struct SubHeadingElement {
    std::string content;
};

struct CaptionElement {
    std::string content;
};

struct ButtonElement {
    std::string label;
    std::function<void()> on_click;
};

struct TextFieldElement {
    std::string value;
    std::function<void(std::string)> on_changed;
    std::optional<std::string> placeholder;
    std::optional<std::string> header;
};

struct CheckBoxElement {
    bool is_checked = false;
    std::function<void(bool)> on_changed;
    std::optional<std::string> label;
};

struct ToggleSwitchElement {
    bool is_on = false;
    std::function<void(bool)> on_changed;
    std::optional<std::string> on_content;
    std::optional<std::string> off_content;
};

struct RadioButtonElement {
    bool is_checked = false;
    std::function<void(bool)> on_changed;
    std::optional<std::string> label;
    std::optional<std::string> group_name;
};

struct SliderElement {
    double value = 0.0;
    double min = 0.0;
    double max = 100.0;
    std::function<void(double)> on_changed;
};

struct ProgressElement {
    double value = 0.0;
    bool is_indeterminate = false;
};

struct ComboBoxElement {
    int selected_index = -1;
    std::vector<std::string> items;
    std::function<void(int)> on_changed;
};

struct StackElement {
    Orientation orientation = Orientation::Vertical;
    double spacing = 0.0;
    std::vector<Element> children;
};

struct GridElement {
    GridDefinition definition;
    std::vector<Element> children;
};

struct BorderElement {
    BoxedElement child;
};

struct ScrollViewElement {
    BoxedElement child;
};

struct ImageElement {
    std::string uri;
};

struct ComponentElement {
    std::shared_ptr<Component> component;
    std::type_index component_type = typeid(void);  // identifies the concrete component class
};

struct FuncElement {
    std::function<Element(RenderContext&)> render;
};

// ListView for virtualized scrolling
struct ListViewElement {
    std::vector<Element> items;
    std::optional<int> selected_index;
    std::function<void(int)> on_selection_changed;
};

// Menu flyout item definition
struct MenuFlyoutItemDef {
    std::string label;
    std::function<void()> on_click;
};

// Flyout button: a button that shows a flyout when clicked
struct FlyoutButtonElement {
    std::string label;
    std::vector<Element> flyout_children; // content inside the flyout
};

// Menu flyout button: a button that shows a menu flyout
struct MenuFlyoutButtonElement {
    std::string label;
    std::vector<MenuFlyoutItemDef> items;
};

// The element variant
using ElementData = std::variant<
    EmptyElement,           // 0
    TextElement,            // 1
    HeadingElement,         // 2
    SubHeadingElement,      // 3
    CaptionElement,         // 4
    ButtonElement,          // 5
    TextFieldElement,       // 6
    CheckBoxElement,        // 7
    ToggleSwitchElement,    // 8
    RadioButtonElement,     // 9
    SliderElement,          // 10
    ProgressElement,        // 11
    ComboBoxElement,        // 12
    StackElement,           // 13
    GridElement,            // 14
    BorderElement,          // 15
    ScrollViewElement,      // 16
    ImageElement,           // 17
    ComponentElement,       // 18
    FuncElement,            // 19
    ListViewElement,        // 20
    FlyoutButtonElement,    // 21
    MenuFlyoutButtonElement // 22
>;

// The main Element type
struct Element {
    ElementData data;
    std::optional<std::string> key;
    std::shared_ptr<ElementModifiers> modifiers;

    // Default: empty element
    Element() : data(EmptyElement{}) {}
    Element(ElementData d) : data(std::move(d)) {}

    // Move/copy
    Element(const Element&) = default;
    Element(Element&&) noexcept = default;
    Element& operator=(const Element&) = default;
    Element& operator=(Element&&) noexcept = default;

    // --- Fluent modifier API (each returns a modified copy) ---

    // Ensure modifiers are allocated (COW)
    Element with_mods() const;

    // Margin
    Element margin(double uniform) const;
    Element margin(double h, double v) const;
    Element margin(double l, double t, double r, double b) const;

    // Padding
    Element padding(double uniform) const;
    Element padding(double h, double v) const;
    Element padding(double l, double t, double r, double b) const;

    // Size
    Element width(double w) const;
    Element height(double h) const;
    Element size(double w, double h) const;
    Element min_width(double w) const;
    Element min_height(double h) const;
    Element max_width(double w) const;
    Element max_height(double h) const;

    // Alignment
    Element h_align(HorizontalAlignment a) const;
    Element v_align(VerticalAlignment a) const;
    Element center() const;

    // Appearance
    Element opacity(double o) const;
    Element background(std::string color) const;
    Element foreground(std::string color) const;
    Element corner_radius(double r) const;

    // State
    Element disabled(bool d = true) const;

    // Typography
    Element font_size(double s) const;
    Element bold() const;
    Element semi_bold() const;

    // Visibility
    Element visible(bool v) const;

    // Key
    Element with_key(std::string k) const;

    // Grid attached properties
    Element grid(int row, int col) const;
    Element grid(int row, int col, int row_span, int col_span) const;

    // Transitions
    Element transition(std::string property, double duration_ms = 300) const;

    // Context menu (right-click)
    Element context_menu(std::vector<std::pair<std::string, std::function<void()>>> items) const;

    // Context menu callback storage (separate from modifiers since functions aren't comparable)
    std::vector<std::function<void()>> context_menu_callbacks;
};

// BoxedElement implementation (needs Element to be complete)
inline BoxedElement::BoxedElement() : ptr(std::make_unique<Element>()) {}
inline BoxedElement::BoxedElement(Element e) : ptr(std::make_unique<Element>(std::move(e))) {}
inline BoxedElement::BoxedElement(const BoxedElement& o) : ptr(std::make_unique<Element>(*o.ptr)) {}
inline BoxedElement& BoxedElement::operator=(const BoxedElement& o) {
    if (this != &o) ptr = std::make_unique<Element>(*o.ptr);
    return *this;
}

} // namespace duct
