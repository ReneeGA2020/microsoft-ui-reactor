#pragma once

#include <optional>
#include <string>

namespace duct {

// Layout types
struct Thickness {
    double left = 0, top = 0, right = 0, bottom = 0;

    Thickness() = default;
    Thickness(double uniform) : left(uniform), top(uniform), right(uniform), bottom(uniform) {}
    Thickness(double h, double v) : left(h), top(v), right(h), bottom(v) {}
    Thickness(double l, double t, double r, double b) : left(l), top(t), right(r), bottom(b) {}

    bool operator==(const Thickness&) const = default;
};

enum class HorizontalAlignment { Left, Center, Right, Stretch };
enum class VerticalAlignment { Top, Center, Bottom, Stretch };
enum class FontWeight { Normal, SemiBold, Bold };
enum class Orientation { Vertical, Horizontal };
enum class Visibility { Visible, Collapsed };

// Grid attached properties
struct GridAttached {
    int row = 0;
    int column = 0;
    int row_span = 1;
    int column_span = 1;

    bool operator==(const GridAttached&) const = default;
};

// Grid definition
struct GridDefinition {
    std::string columns;
    std::string rows;

    bool operator==(const GridDefinition&) const = default;
};

// Implicit transition definition
struct ImplicitTransition {
    std::string property; // "Opacity", "Translation", "Scale", "Rotation"
    double duration_ms = 300;

    bool operator==(const ImplicitTransition&) const = default;
};

// Context menu item definition (for right-click menus attached to any element)
struct ContextMenuItemDef {
    std::string label;
    // Note: std::function is not equality-comparable, so we compare by label only
    bool operator==(const ContextMenuItemDef& o) const { return label == o.label; }
};

// Shared modifiers (COW via shared_ptr for cheap copies)
struct ElementModifiers {
    std::optional<Thickness> margin;
    std::optional<Thickness> padding;
    std::optional<double> width, height;
    std::optional<double> min_width, min_height, max_width, max_height;
    std::optional<HorizontalAlignment> h_align;
    std::optional<VerticalAlignment> v_align;
    std::optional<double> opacity;
    std::optional<std::string> background;
    std::optional<std::string> foreground;
    std::optional<double> corner_radius;
    std::optional<bool> is_enabled;
    std::optional<std::string> tooltip;
    std::optional<double> font_size;
    std::optional<FontWeight> font_weight;
    std::optional<Visibility> visibility;
    std::optional<GridAttached> grid_attached;
    std::optional<std::vector<ImplicitTransition>> transitions;
    std::optional<std::vector<ContextMenuItemDef>> context_menu;

    bool operator==(const ElementModifiers&) const = default;
};

} // namespace duct
