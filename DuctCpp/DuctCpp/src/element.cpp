#include "pch.h"
#include <duct/element.h>

namespace duct {

// Ensure modifiers are allocated, creating a unique copy if shared (COW)
Element Element::with_mods() const {
    Element copy = *this;
    if (!copy.modifiers) {
        copy.modifiers = std::make_shared<ElementModifiers>();
    } else if (copy.modifiers.use_count() > 1) {
        copy.modifiers = std::make_shared<ElementModifiers>(*copy.modifiers);
    }
    return copy;
}

// --- Margin ---
Element Element::margin(double uniform) const {
    auto e = with_mods();
    e.modifiers->margin = Thickness(uniform);
    return e;
}
Element Element::margin(double h, double v) const {
    auto e = with_mods();
    e.modifiers->margin = Thickness(h, v);
    return e;
}
Element Element::margin(double l, double t, double r, double b) const {
    auto e = with_mods();
    e.modifiers->margin = Thickness(l, t, r, b);
    return e;
}

// --- Padding ---
Element Element::padding(double uniform) const {
    auto e = with_mods();
    e.modifiers->padding = Thickness(uniform);
    return e;
}
Element Element::padding(double h, double v) const {
    auto e = with_mods();
    e.modifiers->padding = Thickness(h, v);
    return e;
}
Element Element::padding(double l, double t, double r, double b) const {
    auto e = with_mods();
    e.modifiers->padding = Thickness(l, t, r, b);
    return e;
}

// --- Size ---
Element Element::width(double w) const {
    auto e = with_mods();
    e.modifiers->width = w;
    return e;
}
Element Element::height(double h) const {
    auto e = with_mods();
    e.modifiers->height = h;
    return e;
}
Element Element::size(double w, double h) const {
    auto e = with_mods();
    e.modifiers->width = w;
    e.modifiers->height = h;
    return e;
}
Element Element::min_width(double w) const {
    auto e = with_mods();
    e.modifiers->min_width = w;
    return e;
}
Element Element::min_height(double h) const {
    auto e = with_mods();
    e.modifiers->min_height = h;
    return e;
}
Element Element::max_width(double w) const {
    auto e = with_mods();
    e.modifiers->max_width = w;
    return e;
}
Element Element::max_height(double h) const {
    auto e = with_mods();
    e.modifiers->max_height = h;
    return e;
}

// --- Alignment ---
Element Element::h_align(HorizontalAlignment a) const {
    auto e = with_mods();
    e.modifiers->h_align = a;
    return e;
}
Element Element::v_align(VerticalAlignment a) const {
    auto e = with_mods();
    e.modifiers->v_align = a;
    return e;
}
Element Element::center() const {
    auto e = with_mods();
    e.modifiers->h_align = HorizontalAlignment::Center;
    e.modifiers->v_align = VerticalAlignment::Center;
    return e;
}

// --- Appearance ---
Element Element::opacity(double o) const {
    auto e = with_mods();
    e.modifiers->opacity = o;
    return e;
}
Element Element::background(std::string color) const {
    auto e = with_mods();
    e.modifiers->background = std::move(color);
    return e;
}
Element Element::foreground(std::string color) const {
    auto e = with_mods();
    e.modifiers->foreground = std::move(color);
    return e;
}
Element Element::corner_radius(double r) const {
    auto e = with_mods();
    e.modifiers->corner_radius = r;
    return e;
}

// --- State ---
Element Element::disabled(bool d) const {
    auto e = with_mods();
    e.modifiers->is_enabled = !d;
    return e;
}

// --- Typography ---
Element Element::font_size(double s) const {
    auto e = with_mods();
    e.modifiers->font_size = s;
    return e;
}
Element Element::bold() const {
    auto e = with_mods();
    e.modifiers->font_weight = FontWeight::Bold;
    return e;
}
Element Element::semi_bold() const {
    auto e = with_mods();
    e.modifiers->font_weight = FontWeight::SemiBold;
    return e;
}

// --- Visibility ---
Element Element::visible(bool v) const {
    auto e = with_mods();
    e.modifiers->visibility = v ? Visibility::Visible : Visibility::Collapsed;
    return e;
}

// --- Key ---
Element Element::with_key(std::string k) const {
    Element copy = *this;
    copy.key = std::move(k);
    return copy;
}

// --- Grid attached properties ---
Element Element::grid(int row, int col) const {
    auto e = with_mods();
    e.modifiers->grid_attached = GridAttached{ row, col, 1, 1 };
    return e;
}
Element Element::grid(int row, int col, int row_span, int col_span) const {
    auto e = with_mods();
    e.modifiers->grid_attached = GridAttached{ row, col, row_span, col_span };
    return e;
}

// --- Transitions ---
Element Element::transition(std::string property, double duration_ms) const {
    auto e = with_mods();
    if (!e.modifiers->transitions) {
        e.modifiers->transitions = std::vector<ImplicitTransition>{};
    }
    e.modifiers->transitions->push_back(ImplicitTransition{ std::move(property), duration_ms });
    return e;
}

// --- Context menu ---
Element Element::context_menu(std::vector<std::pair<std::string, std::function<void()>>> items) const {
    auto e = with_mods();
    std::vector<ContextMenuItemDef> defs;
    std::vector<std::function<void()>> callbacks;
    for (auto& [label, fn] : items) {
        defs.push_back(ContextMenuItemDef{ label });
        callbacks.push_back(std::move(fn));
    }
    e.modifiers->context_menu = std::move(defs);
    e.context_menu_callbacks = std::move(callbacks);
    return e;
}

} // namespace duct
