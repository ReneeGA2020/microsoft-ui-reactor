#include "pch.h"
#include "reconciler.h"
#include <winrt/Microsoft.UI.Xaml.Media.h>
#include <winrt/Microsoft.UI.Xaml.Media.Imaging.h>

#include <winrt/Microsoft.UI.Xaml.Controls.Primitives.h>
#include <winrt/Windows.UI.Text.h>

namespace duct {

namespace media = winrt::Microsoft::UI::Xaml::Media;

// Helper: convert std::string to winrt::hstring
static winrt::hstring to_hstring(const std::string& s) {
    if (s.empty()) return L"";
    int size = MultiByteToWideChar(CP_UTF8, 0, s.data(), static_cast<int>(s.size()), nullptr, 0);
    std::wstring ws(size, 0);
    MultiByteToWideChar(CP_UTF8, 0, s.data(), static_cast<int>(s.size()), ws.data(), size);
    return winrt::hstring(ws);
}

// Construct a Color struct directly (avoids ColorHelper activation)
static winrt::Windows::UI::Color make_color(uint8_t a, uint8_t r, uint8_t g, uint8_t b) {
    return { a, r, g, b };
}

// Parse a color string to an ARGB value
static winrt::Windows::UI::Color parse_color(const std::string& color) {
    // Support hex colors: #RRGGBB or #AARRGGBB
    if (color.size() >= 7 && color[0] == '#') {
        uint8_t a = 255, r, g, b;
        if (color.size() == 9) {
            a = static_cast<uint8_t>(std::stoul(color.substr(1, 2), nullptr, 16));
            r = static_cast<uint8_t>(std::stoul(color.substr(3, 2), nullptr, 16));
            g = static_cast<uint8_t>(std::stoul(color.substr(5, 2), nullptr, 16));
            b = static_cast<uint8_t>(std::stoul(color.substr(7, 2), nullptr, 16));
        } else {
            r = static_cast<uint8_t>(std::stoul(color.substr(1, 2), nullptr, 16));
            g = static_cast<uint8_t>(std::stoul(color.substr(3, 2), nullptr, 16));
            b = static_cast<uint8_t>(std::stoul(color.substr(5, 2), nullptr, 16));
        }
        return make_color(a, r, g, b);
    }
    // Named colors — common subset
    if (color == "red") return make_color(255, 255, 0, 0);
    if (color == "green") return make_color(255, 0, 128, 0);
    if (color == "blue") return make_color(255, 0, 0, 255);
    if (color == "white") return make_color(255, 255, 255, 255);
    if (color == "black") return make_color(255, 0, 0, 0);
    if (color == "gray" || color == "grey") return make_color(255, 128, 128, 128);
    if (color == "transparent") return make_color(0, 0, 0, 0);
    if (color == "yellow") return make_color(255, 255, 255, 0);
    if (color == "orange") return make_color(255, 255, 165, 0);
    if (color == "purple") return make_color(255, 128, 0, 128);
    if (color == "cyan") return make_color(255, 0, 255, 255);
    if (color == "magenta") return make_color(255, 255, 0, 255);

    // Default: transparent
    return make_color(0, 0, 0, 0);
}

// Brush cache: color string → SolidColorBrush (parsed once, reused)
static std::unordered_map<std::string, media::SolidColorBrush>& brush_cache() {
    static std::unordered_map<std::string, media::SolidColorBrush> cache;
    return cache;
}

static media::SolidColorBrush parse_brush(const std::string& color) {
    auto& cache = brush_cache();
    auto it = cache.find(color);
    if (it != cache.end()) return it->second;

    auto brush = media::SolidColorBrush(parse_color(color));
    cache[color] = brush;
    return brush;
}

// Apply modifiers to a control
void Reconciler::apply_modifiers(xaml::UIElement control,
                                  const std::shared_ptr<ElementModifiers>& mods) {
    if (!mods || !control) return;

    auto fe = control.try_as<xaml::FrameworkElement>();

    if (mods->margin && fe) {
        auto& m = *mods->margin;
        fe.Margin({ m.left, m.top, m.right, m.bottom });
    }
    if (mods->width && fe) fe.Width(*mods->width);
    if (mods->height && fe) fe.Height(*mods->height);
    if (mods->min_width && fe) fe.MinWidth(*mods->min_width);
    if (mods->min_height && fe) fe.MinHeight(*mods->min_height);
    if (mods->max_width && fe) fe.MaxWidth(*mods->max_width);
    if (mods->max_height && fe) fe.MaxHeight(*mods->max_height);

    if (mods->h_align && fe) {
        switch (*mods->h_align) {
            case HorizontalAlignment::Left: fe.HorizontalAlignment(xaml::HorizontalAlignment::Left); break;
            case HorizontalAlignment::Center: fe.HorizontalAlignment(xaml::HorizontalAlignment::Center); break;
            case HorizontalAlignment::Right: fe.HorizontalAlignment(xaml::HorizontalAlignment::Right); break;
            case HorizontalAlignment::Stretch: fe.HorizontalAlignment(xaml::HorizontalAlignment::Stretch); break;
        }
    }
    if (mods->v_align && fe) {
        switch (*mods->v_align) {
            case VerticalAlignment::Top: fe.VerticalAlignment(xaml::VerticalAlignment::Top); break;
            case VerticalAlignment::Center: fe.VerticalAlignment(xaml::VerticalAlignment::Center); break;
            case VerticalAlignment::Bottom: fe.VerticalAlignment(xaml::VerticalAlignment::Bottom); break;
            case VerticalAlignment::Stretch: fe.VerticalAlignment(xaml::VerticalAlignment::Stretch); break;
        }
    }

    if (mods->opacity) control.Opacity(*mods->opacity);
    if (mods->visibility) {
        control.Visibility(*mods->visibility == Visibility::Visible
            ? xaml::Visibility::Visible : xaml::Visibility::Collapsed);
    }

    if (mods->is_enabled) {
        if (auto c = control.try_as<controls::Control>()) {
            c.IsEnabled(*mods->is_enabled);
        }
    }

    if (mods->padding) {
        auto& p = *mods->padding;
        if (auto c = control.try_as<controls::Control>()) {
            c.Padding({ p.left, p.top, p.right, p.bottom });
        } else if (auto b = control.try_as<controls::Border>()) {
            b.Padding({ p.left, p.top, p.right, p.bottom });
        }
    }

    if (mods->background) {
        auto brush = parse_brush(*mods->background);
        if (auto c = control.try_as<controls::Control>()) {
            c.Background(brush);
        } else if (auto p = control.try_as<controls::Panel>()) {
            p.Background(brush);
        } else if (auto b = control.try_as<controls::Border>()) {
            b.Background(brush);
        }
    }

    if (mods->foreground) {
        auto brush = parse_brush(*mods->foreground);
        if (auto c = control.try_as<controls::Control>()) {
            c.Foreground(brush);
        } else if (auto tb = control.try_as<controls::TextBlock>()) {
            tb.Foreground(brush);
        }
    }

    if (mods->corner_radius) {
        double r = *mods->corner_radius;
        winrt::Microsoft::UI::Xaml::CornerRadius cr{ r, r, r, r };
        if (auto c = control.try_as<controls::Control>()) {
            c.CornerRadius(cr);
        } else if (auto b = control.try_as<controls::Border>()) {
            b.CornerRadius(cr);
        }
    }

    if (mods->font_size) {
        if (auto tb = control.try_as<controls::TextBlock>()) {
            tb.FontSize(*mods->font_size);
        } else if (auto c = control.try_as<controls::Control>()) {
            c.FontSize(*mods->font_size);
        }
    }

    if (mods->font_weight) {
        winrt::Windows::UI::Text::FontWeight fw;
        switch (*mods->font_weight) {
            case FontWeight::Normal: fw = winrt::Windows::UI::Text::FontWeights::Normal(); break;
            case FontWeight::SemiBold: fw = winrt::Windows::UI::Text::FontWeights::SemiBold(); break;
            case FontWeight::Bold: fw = winrt::Windows::UI::Text::FontWeights::Bold(); break;
        }
        if (auto tb = control.try_as<controls::TextBlock>()) {
            tb.FontWeight(fw);
        } else if (auto c = control.try_as<controls::Control>()) {
            c.FontWeight(fw);
        }
    }

    // Grid attached properties
    if (mods->grid_attached) {
        auto& ga = *mods->grid_attached;
        controls::Grid::SetRow(fe, ga.row);
        controls::Grid::SetColumn(fe, ga.column);
        if (ga.row_span > 1) controls::Grid::SetRowSpan(fe, ga.row_span);
        if (ga.column_span > 1) controls::Grid::SetColumnSpan(fe, ga.column_span);
    }

    // Implicit transitions
    if (mods->transitions) {
        for (const auto& t : *mods->transitions) {
            if (t.property == "Opacity") {
                auto st = winrt::Microsoft::UI::Xaml::ScalarTransition();
                st.Duration(std::chrono::milliseconds(static_cast<int>(t.duration_ms)));
                control.OpacityTransition(st);
            }
        }
    }

}

// Apply modifier diff — only set changed properties (per-property comparison)
void Reconciler::apply_modifiers_diff(xaml::UIElement control,
                                       const std::shared_ptr<ElementModifiers>& old_mods,
                                       const std::shared_ptr<ElementModifiers>& new_mods) {
    // Same pointer (COW) — nothing changed
    if (old_mods == new_mods) return;

    // No new modifiers — nothing to apply
    if (!new_mods) return;

    // No old modifiers — apply everything fresh
    if (!old_mods) {
        apply_modifiers(control, new_mods);
        return;
    }

    // Fast path: struct-level equality means nothing changed
    if (*old_mods == *new_mods) return;

    // Per-property diff: only call into WinRT for fields that actually differ
    auto& o = *old_mods;
    auto& n = *new_mods;

    auto fe = control.try_as<xaml::FrameworkElement>();

    if (n.margin != o.margin && n.margin && fe) {
        auto& m = *n.margin;
        fe.Margin({ m.left, m.top, m.right, m.bottom });
    }
    if (n.width != o.width && n.width && fe) fe.Width(*n.width);
    if (n.height != o.height && n.height && fe) fe.Height(*n.height);
    if (n.min_width != o.min_width && n.min_width && fe) fe.MinWidth(*n.min_width);
    if (n.min_height != o.min_height && n.min_height && fe) fe.MinHeight(*n.min_height);
    if (n.max_width != o.max_width && n.max_width && fe) fe.MaxWidth(*n.max_width);
    if (n.max_height != o.max_height && n.max_height && fe) fe.MaxHeight(*n.max_height);

    if (n.h_align != o.h_align && n.h_align && fe) {
        switch (*n.h_align) {
            case HorizontalAlignment::Left: fe.HorizontalAlignment(xaml::HorizontalAlignment::Left); break;
            case HorizontalAlignment::Center: fe.HorizontalAlignment(xaml::HorizontalAlignment::Center); break;
            case HorizontalAlignment::Right: fe.HorizontalAlignment(xaml::HorizontalAlignment::Right); break;
            case HorizontalAlignment::Stretch: fe.HorizontalAlignment(xaml::HorizontalAlignment::Stretch); break;
        }
    }
    if (n.v_align != o.v_align && n.v_align && fe) {
        switch (*n.v_align) {
            case VerticalAlignment::Top: fe.VerticalAlignment(xaml::VerticalAlignment::Top); break;
            case VerticalAlignment::Center: fe.VerticalAlignment(xaml::VerticalAlignment::Center); break;
            case VerticalAlignment::Bottom: fe.VerticalAlignment(xaml::VerticalAlignment::Bottom); break;
            case VerticalAlignment::Stretch: fe.VerticalAlignment(xaml::VerticalAlignment::Stretch); break;
        }
    }

    if (n.opacity != o.opacity && n.opacity) control.Opacity(*n.opacity);
    if (n.visibility != o.visibility && n.visibility) {
        control.Visibility(*n.visibility == Visibility::Visible
            ? xaml::Visibility::Visible : xaml::Visibility::Collapsed);
    }

    if (n.is_enabled != o.is_enabled && n.is_enabled) {
        if (auto c = control.try_as<controls::Control>()) c.IsEnabled(*n.is_enabled);
    }

    if (n.padding != o.padding && n.padding) {
        auto& p = *n.padding;
        if (auto c = control.try_as<controls::Control>()) {
            c.Padding({ p.left, p.top, p.right, p.bottom });
        } else if (auto b = control.try_as<controls::Border>()) {
            b.Padding({ p.left, p.top, p.right, p.bottom });
        }
    }

    if (n.background != o.background && n.background) {
        auto brush = parse_brush(*n.background);
        if (auto c = control.try_as<controls::Control>()) {
            c.Background(brush);
        } else if (auto p = control.try_as<controls::Panel>()) {
            p.Background(brush);
        } else if (auto b = control.try_as<controls::Border>()) {
            b.Background(brush);
        }
    }

    if (n.foreground != o.foreground && n.foreground) {
        auto brush = parse_brush(*n.foreground);
        if (auto c = control.try_as<controls::Control>()) {
            c.Foreground(brush);
        } else if (auto tb = control.try_as<controls::TextBlock>()) {
            tb.Foreground(brush);
        }
    }

    if (n.corner_radius != o.corner_radius && n.corner_radius) {
        double r = *n.corner_radius;
        winrt::Microsoft::UI::Xaml::CornerRadius cr{ r, r, r, r };
        if (auto c = control.try_as<controls::Control>()) {
            c.CornerRadius(cr);
        } else if (auto b = control.try_as<controls::Border>()) {
            b.CornerRadius(cr);
        }
    }

    if (n.font_size != o.font_size && n.font_size) {
        if (auto tb = control.try_as<controls::TextBlock>()) {
            tb.FontSize(*n.font_size);
        } else if (auto c = control.try_as<controls::Control>()) {
            c.FontSize(*n.font_size);
        }
    }

    if (n.font_weight != o.font_weight && n.font_weight) {
        winrt::Windows::UI::Text::FontWeight fw;
        switch (*n.font_weight) {
            case FontWeight::Normal: fw = winrt::Windows::UI::Text::FontWeights::Normal(); break;
            case FontWeight::SemiBold: fw = winrt::Windows::UI::Text::FontWeights::SemiBold(); break;
            case FontWeight::Bold: fw = winrt::Windows::UI::Text::FontWeights::Bold(); break;
        }
        if (auto tb = control.try_as<controls::TextBlock>()) {
            tb.FontWeight(fw);
        } else if (auto c = control.try_as<controls::Control>()) {
            c.FontWeight(fw);
        }
    }

    if (n.grid_attached != o.grid_attached && n.grid_attached && fe) {
        auto& ga = *n.grid_attached;
        controls::Grid::SetRow(fe, ga.row);
        controls::Grid::SetColumn(fe, ga.column);
        if (ga.row_span > 1) controls::Grid::SetRowSpan(fe, ga.row_span);
        if (ga.column_span > 1) controls::Grid::SetColumnSpan(fe, ga.column_span);
    }

    if (n.transitions != o.transitions && n.transitions) {
        for (const auto& t : *n.transitions) {
            if (t.property == "Opacity") {
                auto st = winrt::Microsoft::UI::Xaml::ScalarTransition();
                st.Duration(std::chrono::milliseconds(static_cast<int>(t.duration_ms)));
                control.OpacityTransition(st);
            }
        }
    }
}

// Helper: set WinUI font weight from our enum
static winrt::Windows::UI::Text::FontWeight to_winrt_font_weight(FontWeight fw) {
    switch (fw) {
        case FontWeight::SemiBold: return winrt::Windows::UI::Text::FontWeights::SemiBold();
        case FontWeight::Bold: return winrt::Windows::UI::Text::FontWeights::Bold();
        default: return winrt::Windows::UI::Text::FontWeights::Normal();
    }
}

// Helper: parse grid definitions like "* 2* Auto 100"
static void parse_grid_defs(const std::string& defs,
                             winrt::Windows::Foundation::Collections::IVector<controls::ColumnDefinition> cols,
                             bool is_columns) {
    // Simple tokenizer
    std::istringstream iss(defs);
    std::string token;
    while (iss >> token) {
        if (is_columns) {
            controls::ColumnDefinition cd;
            if (token == "Auto") {
                cd.Width({ 0, xaml::GridUnitType::Auto });
            } else if (token.back() == '*') {
                double val = (token.size() == 1) ? 1.0 : std::stod(token.substr(0, token.size() - 1));
                cd.Width({ val, xaml::GridUnitType::Star });
            } else if (token == "*") {
                cd.Width({ 1.0, xaml::GridUnitType::Star });
            } else {
                cd.Width({ std::stod(token), xaml::GridUnitType::Pixel });
            }
            cols.Append(cd);
        }
    }
}

static void parse_row_defs(const std::string& defs,
                            winrt::Windows::Foundation::Collections::IVector<controls::RowDefinition> rows) {
    std::istringstream iss(defs);
    std::string token;
    while (iss >> token) {
        controls::RowDefinition rd;
        if (token == "Auto") {
            rd.Height({ 0, xaml::GridUnitType::Auto });
        } else if (token.back() == '*') {
            double val = (token.size() == 1) ? 1.0 : std::stod(token.substr(0, token.size() - 1));
            rd.Height({ val, xaml::GridUnitType::Star });
        } else if (token == "*") {
            rd.Height({ 1.0, xaml::GridUnitType::Star });
        } else {
            rd.Height({ std::stod(token), xaml::GridUnitType::Pixel });
        }
        rows.Append(rd);
    }
}

// ════════════════════════════════════════════════════════════════════
//  Mount dispatch
// ════════════════════════════════════════════════════════════════════

xaml::UIElement Reconciler::mount(const Element& el, std::function<void()> request_rerender) {
    using namespace winrt::Microsoft::UI::Xaml;

    auto control = std::visit([&](const auto& data) -> xaml::UIElement {
        using T = std::decay_t<decltype(data)>;

        if constexpr (std::is_same_v<T, EmptyElement>) {
            return nullptr;
        }
        else if constexpr (std::is_same_v<T, TextElement>) {
            controls::TextBlock tb;
            tb.Text(to_hstring(data.content));
            if (data.font_size) tb.FontSize(*data.font_size);
            if (data.font_weight) tb.FontWeight(to_winrt_font_weight(*data.font_weight));
            tb.TextWrapping(TextWrapping::Wrap);
            return tb;
        }
        else if constexpr (std::is_same_v<T, HeadingElement>) {
            controls::TextBlock tb;
            tb.Text(to_hstring(data.content));
            tb.FontSize(28);
            tb.FontWeight(winrt::Windows::UI::Text::FontWeights::SemiBold());
            tb.TextWrapping(TextWrapping::Wrap);
            return tb;
        }
        else if constexpr (std::is_same_v<T, SubHeadingElement>) {
            controls::TextBlock tb;
            tb.Text(to_hstring(data.content));
            tb.FontSize(20);
            tb.FontWeight(winrt::Windows::UI::Text::FontWeights::SemiBold());
            tb.TextWrapping(TextWrapping::Wrap);
            return tb;
        }
        else if constexpr (std::is_same_v<T, CaptionElement>) {
            controls::TextBlock tb;
            tb.Text(to_hstring(data.content));
            tb.FontSize(12);
            tb.Opacity(0.6);
            tb.TextWrapping(TextWrapping::Wrap);
            return tb;
        }
        else if constexpr (std::is_same_v<T, ButtonElement>) {
            controls::Button btn;
            btn.Content(winrt::box_value(to_hstring(data.label)));
            // shared_ptr callback: event handler dereferences it each time,
            // update() swaps the target to keep callbacks fresh
            auto cb = std::make_shared<std::function<void()>>(data.on_click);
            btn.Tag(winrt::box_value(reinterpret_cast<uint64_t>(cb.get())));
            btn.Click([cb](winrt::Windows::Foundation::IInspectable const&,
                           xaml::RoutedEventArgs const&) {
                if (*cb) (*cb)();
            });
            return btn;
        }
        else if constexpr (std::is_same_v<T, TextFieldElement>) {
            controls::TextBox tb;
            tb.Text(to_hstring(data.value));
            if (data.placeholder) tb.PlaceholderText(to_hstring(*data.placeholder));
            if (data.header) tb.Header(winrt::box_value(to_hstring(*data.header)));
            auto cb = std::make_shared<std::function<void(std::string)>>(data.on_changed);
            tb.Tag(winrt::box_value(reinterpret_cast<uint64_t>(cb.get())));
            tb.TextChanged([cb](winrt::Windows::Foundation::IInspectable const& sender,
                                controls::TextChangedEventArgs const&) {
                if (*cb) {
                    auto box = sender.as<controls::TextBox>();
                    (*cb)(winrt::to_string(box.Text()));
                }
            });
            return tb;
        }
        else if constexpr (std::is_same_v<T, CheckBoxElement>) {
            controls::CheckBox cb;
            cb.IsChecked(data.is_checked);
            if (data.label) cb.Content(winrt::box_value(to_hstring(*data.label)));
            auto fn = std::make_shared<std::function<void(bool)>>(data.on_changed);
            cb.Tag(winrt::box_value(reinterpret_cast<uint64_t>(fn.get())));
            cb.Checked([fn](winrt::Windows::Foundation::IInspectable const&,
                            xaml::RoutedEventArgs const&) {
                if (*fn) (*fn)(true);
            });
            cb.Unchecked([fn](winrt::Windows::Foundation::IInspectable const&,
                              xaml::RoutedEventArgs const&) {
                if (*fn) (*fn)(false);
            });
            return cb;
        }
        else if constexpr (std::is_same_v<T, ToggleSwitchElement>) {
            controls::ToggleSwitch ts;
            ts.IsOn(data.is_on);
            if (data.on_content) ts.OnContent(winrt::box_value(to_hstring(*data.on_content)));
            if (data.off_content) ts.OffContent(winrt::box_value(to_hstring(*data.off_content)));
            auto fn = std::make_shared<std::function<void(bool)>>(data.on_changed);
            ts.Tag(winrt::box_value(reinterpret_cast<uint64_t>(fn.get())));
            ts.Toggled([fn](winrt::Windows::Foundation::IInspectable const& sender,
                            xaml::RoutedEventArgs const&) {
                if (*fn) {
                    auto toggle = sender.as<controls::ToggleSwitch>();
                    (*fn)(toggle.IsOn());
                }
            });
            return ts;
        }
        else if constexpr (std::is_same_v<T, RadioButtonElement>) {
            controls::RadioButton rb;
            rb.IsChecked(data.is_checked);
            if (data.label) rb.Content(winrt::box_value(to_hstring(*data.label)));
            if (data.group_name) rb.GroupName(to_hstring(*data.group_name));
            auto fn = std::make_shared<std::function<void(bool)>>(data.on_changed);
            rb.Tag(winrt::box_value(reinterpret_cast<uint64_t>(fn.get())));
            rb.Checked([fn](winrt::Windows::Foundation::IInspectable const&,
                            xaml::RoutedEventArgs const&) {
                if (*fn) (*fn)(true);
            });
            return rb;
        }
        else if constexpr (std::is_same_v<T, SliderElement>) {
            controls::Slider sl;
            auto fn = std::make_shared<std::function<void(double)>>(data.on_changed);
            sl.Tag(winrt::box_value(reinterpret_cast<uint64_t>(fn.get())));
            sl.Minimum(data.min);
            sl.Maximum(data.max);
            sl.Value(data.value);
            sl.ValueChanged([fn](winrt::Windows::Foundation::IInspectable const&,
                                 controls::Primitives::RangeBaseValueChangedEventArgs const& args) {
                if (*fn) (*fn)(args.NewValue());
            });
            return sl;
        }
        else if constexpr (std::is_same_v<T, ProgressElement>) {
            controls::ProgressBar pb;
            pb.IsIndeterminate(data.is_indeterminate);
            if (!data.is_indeterminate) {
                pb.Minimum(0);
                pb.Maximum(1);
                pb.Value(data.value);
            }
            return pb;
        }
        else if constexpr (std::is_same_v<T, ComboBoxElement>) {
            controls::ComboBox cb;
            for (const auto& item : data.items) {
                cb.Items().Append(winrt::box_value(to_hstring(item)));
            }
            auto fn = std::make_shared<std::function<void(int)>>(data.on_changed);
            cb.Tag(winrt::box_value(reinterpret_cast<uint64_t>(fn.get())));
            if (data.selected_index >= 0) cb.SelectedIndex(data.selected_index);
            cb.SelectionChanged([fn](winrt::Windows::Foundation::IInspectable const& sender,
                                     controls::SelectionChangedEventArgs const&) {
                if (*fn) {
                    auto combo = sender.as<controls::ComboBox>();
                    (*fn)(combo.SelectedIndex());
                }
            });
            return cb;
        }
        else if constexpr (std::is_same_v<T, StackElement>) {
            controls::StackPanel sp;
            sp.Orientation(data.orientation == Orientation::Vertical
                ? controls::Orientation::Vertical : controls::Orientation::Horizontal);
            sp.Spacing(data.spacing);
            for (const auto& child : data.children) {
                auto child_control = mount(child, request_rerender);
                if (child_control) sp.Children().Append(child_control);
            }
            return sp;
        }
        else if constexpr (std::is_same_v<T, GridElement>) {
            controls::Grid grid;
            if (!data.definition.columns.empty()) {
                parse_grid_defs(data.definition.columns, grid.ColumnDefinitions(), true);
            }
            if (!data.definition.rows.empty()) {
                parse_row_defs(data.definition.rows, grid.RowDefinitions());
            }
            for (const auto& child : data.children) {
                auto child_control = mount(child, request_rerender);
                if (child_control) {
                    // Apply grid attached props from the child's modifiers
                    if (child.modifiers && child.modifiers->grid_attached) {
                        auto& ga = *child.modifiers->grid_attached;
                        auto fe = child_control.try_as<xaml::FrameworkElement>();
                        if (fe) {
                            controls::Grid::SetRow(fe, ga.row);
                            controls::Grid::SetColumn(fe, ga.column);
                            if (ga.row_span > 1) controls::Grid::SetRowSpan(fe, ga.row_span);
                            if (ga.column_span > 1) controls::Grid::SetColumnSpan(fe, ga.column_span);
                        }
                    }
                    grid.Children().Append(child_control);
                }
            }
            return grid;
        }
        else if constexpr (std::is_same_v<T, BorderElement>) {
            controls::Border border;
            auto child_control = mount(*data.child, request_rerender);
            if (child_control) border.Child(child_control);
            return border;
        }
        else if constexpr (std::is_same_v<T, ScrollViewElement>) {
            controls::ScrollViewer sv;
            auto child_control = mount(*data.child, request_rerender);
            if (child_control) sv.Content(child_control);
            return sv;
        }
        else if constexpr (std::is_same_v<T, ImageElement>) {
            controls::Image img;
            media::Imaging::BitmapImage bmp;
            bmp.UriSource(winrt::Windows::Foundation::Uri(to_hstring(data.uri)));
            img.Source(bmp);
            return img;
        }
        else if constexpr (std::is_same_v<T, ComponentElement>) {
            // Wrap in a Border as identity anchor
            controls::Border wrapper;
            auto comp = data.component;

            // Initialize context with render callback
            comp->begin_render();
            comp->context().set_request_render(request_rerender);
            auto child_element = comp->render();
            comp->flush_effects();

            auto child_control = mount(child_element, request_rerender);
            if (child_control) wrapper.Child(child_control);

            // Track the component node with a unique ID in the Tag
            uint64_t id = next_component_id_++;
            set_component_id(wrapper, id);
            component_nodes_[id] = ComponentNode{
                comp,
                nullptr,
                std::move(child_element),
                child_control
            };

            return wrapper;
        }
        else if constexpr (std::is_same_v<T, FuncElement>) {
            // Wrap in a Border as identity anchor
            controls::Border wrapper;

            auto ctx = std::make_unique<RenderContext>();
            ctx->set_request_render(request_rerender);
            ctx->begin_render();
            auto child_element = data.render(*ctx);
            ctx->flush_effects();

            auto child_control = mount(child_element, request_rerender);
            if (child_control) wrapper.Child(child_control);

            // Track the func component node with a unique ID
            uint64_t id = next_component_id_++;
            set_component_id(wrapper, id);
            component_nodes_[id] = ComponentNode{
                nullptr,
                std::move(ctx),
                std::move(child_element),
                child_control
            };

            return wrapper;
        }
        else if constexpr (std::is_same_v<T, ListViewElement>) {
            controls::ListView lv;
            lv.SelectionMode(controls::ListViewSelectionMode::Single);
            for (const auto& item : data.items) {
                auto item_control = mount(item, request_rerender);
                if (item_control) {
                    controls::ListViewItem lvi;
                    lvi.Content(item_control);
                    lv.Items().Append(lvi);
                }
            }
            if (data.selected_index && *data.selected_index >= 0) {
                lv.SelectedIndex(*data.selected_index);
            }
            if (data.on_selection_changed) {
                auto fn = std::make_shared<std::function<void(int)>>(data.on_selection_changed);
                lv.Tag(winrt::box_value(reinterpret_cast<uint64_t>(fn.get())));
                lv.SelectionChanged([fn](winrt::Windows::Foundation::IInspectable const& sender,
                                         controls::SelectionChangedEventArgs const&) {
                    if (*fn) {
                        auto list = sender.as<controls::ListView>();
                        (*fn)(list.SelectedIndex());
                    }
                });
            }
            return lv;
        }
        else if constexpr (std::is_same_v<T, FlyoutButtonElement>) {
            controls::Button btn;
            btn.Content(winrt::box_value(to_hstring(data.label)));

            // Build flyout content
            controls::Flyout flyout;
            if (data.flyout_children.size() == 1) {
                auto child_control = mount(data.flyout_children[0], request_rerender);
                if (child_control) flyout.Content(child_control);
            } else {
                controls::StackPanel sp;
                sp.Spacing(8);
                for (const auto& child : data.flyout_children) {
                    auto child_control = mount(child, request_rerender);
                    if (child_control) sp.Children().Append(child_control);
                }
                flyout.Content(sp);
            }
            btn.Flyout(flyout);
            return btn;
        }
        else if constexpr (std::is_same_v<T, MenuFlyoutButtonElement>) {
            controls::Button btn;
            btn.Content(winrt::box_value(to_hstring(data.label)));

            controls::MenuFlyout mf;
            for (const auto& item : data.items) {
                controls::MenuFlyoutItem mfi;
                mfi.Text(to_hstring(item.label));
                auto fn = std::make_shared<std::function<void()>>(item.on_click);
                mfi.Click([fn](winrt::Windows::Foundation::IInspectable const&,
                               xaml::RoutedEventArgs const&) {
                    if (*fn) (*fn)();
                });
                mf.Items().Append(mfi);
            }
            btn.Flyout(mf);
            return btn;
        }
        else {
            static_assert(sizeof(T) == 0, "Unhandled element type in mount");
            return nullptr;
        }
    }, el.data);

    if (control) {
        apply_modifiers(control, el.modifiers);

        // Context menu (needs both modifier defs and element callbacks)
        if (el.modifiers && el.modifiers->context_menu && !el.context_menu_callbacks.empty()) {
            if (auto fe = control.try_as<xaml::FrameworkElement>()) {
                controls::MenuFlyout mf;
                auto& defs = *el.modifiers->context_menu;
                auto callbacks_ptr = std::make_shared<std::vector<std::function<void()>>>(el.context_menu_callbacks);
                for (size_t i = 0; i < defs.size() && i < callbacks_ptr->size(); ++i) {
                    controls::MenuFlyoutItem mfi;
                    mfi.Text(to_hstring(defs[i].label));
                    auto idx = i;
                    mfi.Click([callbacks_ptr, idx](winrt::Windows::Foundation::IInspectable const&,
                                                    xaml::RoutedEventArgs const&) {
                        if (idx < callbacks_ptr->size() && (*callbacks_ptr)[idx]) {
                            (*callbacks_ptr)[idx]();
                        }
                    });
                    mf.Items().Append(mfi);
                }
                fe.ContextFlyout(mf);
            }
        }
    }
    return control;
}

} // namespace duct
