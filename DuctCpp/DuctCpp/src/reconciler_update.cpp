#include "pch.h"
#include "reconciler.h"
#include <sstream>
#include <winrt/Microsoft.UI.Xaml.Media.h>
#include <winrt/Microsoft.UI.Xaml.Media.Imaging.h>

#include <winrt/Microsoft.UI.Xaml.Controls.Primitives.h>
#include <winrt/Windows.UI.Text.h>

namespace duct {

// Direct wstring to hstring — no conversion needed
static winrt::hstring to_hstring(const std::wstring& s) {
    return winrt::hstring(s);
}

// Helper: update the shared callback stored in a control's Tag
template<typename F>
static void update_callback(xaml::UIElement control, const F& new_fn) {
    if (auto fe = control.try_as<xaml::FrameworkElement>()) {
        if (auto tag = fe.Tag()) {
            auto ptr = winrt::unbox_value_or<uint64_t>(tag, 0);
            if (ptr) *reinterpret_cast<F*>(ptr) = new_fn;
        }
    }
}

xaml::UIElement Reconciler::update(
    const Element& old_el,
    const Element& new_el,
    xaml::UIElement control,
    std::function<void()> request_rerender)
{
    // Visit the old+new pair. Since can_update ensures same index, we can visit new_el.
    std::visit([&](const auto& new_data) {
        using T = std::decay_t<decltype(new_data)>;

        if constexpr (std::is_same_v<T, EmptyElement>) {
            // Nothing to update
        }
        else if constexpr (std::is_same_v<T, TextElement>) {
            auto tb = control.as<controls::TextBlock>();
            auto& old_data = std::get<TextElement>(old_el.data);
            if (old_data.content != new_data.content) {
#ifdef DUCT_DEBUG_LOG
                OutputDebugStringW((L"UPDATE text: '" + old_data.content + L"' -> '" + new_data.content + L"'\n").c_str());
#endif
                tb.Text(to_hstring(new_data.content));
            }
            if (old_data.font_size != new_data.font_size && new_data.font_size) tb.FontSize(*new_data.font_size);
            if (old_data.font_weight != new_data.font_weight && new_data.font_weight) {
                winrt::Windows::UI::Text::FontWeight fw;
                switch (*new_data.font_weight) {
                    case FontWeight::SemiBold: fw = winrt::Windows::UI::Text::FontWeights::SemiBold(); break;
                    case FontWeight::Bold: fw = winrt::Windows::UI::Text::FontWeights::Bold(); break;
                    default: fw = winrt::Windows::UI::Text::FontWeights::Normal(); break;
                }
                tb.FontWeight(fw);
            }
        }
        else if constexpr (std::is_same_v<T, HeadingElement>) {
            auto tb = control.as<controls::TextBlock>();
            auto& old_data = std::get<HeadingElement>(old_el.data);
            if (old_data.content != new_data.content) tb.Text(to_hstring(new_data.content));
        }
        else if constexpr (std::is_same_v<T, SubHeadingElement>) {
            auto tb = control.as<controls::TextBlock>();
            auto& old_data = std::get<SubHeadingElement>(old_el.data);
            if (old_data.content != new_data.content) tb.Text(to_hstring(new_data.content));
        }
        else if constexpr (std::is_same_v<T, CaptionElement>) {
            auto tb = control.as<controls::TextBlock>();
            auto& old_data = std::get<CaptionElement>(old_el.data);
            if (old_data.content != new_data.content) tb.Text(to_hstring(new_data.content));
        }
        else if constexpr (std::is_same_v<T, ButtonElement>) {
            auto btn = control.as<controls::Button>();
            auto& old_data = std::get<ButtonElement>(old_el.data);
            if (old_data.label != new_data.label)
                btn.Content(winrt::box_value(to_hstring(new_data.label)));
            update_callback<std::function<void()>>(control, new_data.on_click);
        }
        else if constexpr (std::is_same_v<T, TextFieldElement>) {
            auto tb = control.as<controls::TextBox>();
            auto& old_data = std::get<TextFieldElement>(old_el.data);
            if (old_data.value != new_data.value) tb.Text(to_hstring(new_data.value));
            if (old_data.placeholder != new_data.placeholder && new_data.placeholder)
                tb.PlaceholderText(to_hstring(*new_data.placeholder));
            if (old_data.header != new_data.header && new_data.header)
                tb.Header(winrt::box_value(to_hstring(*new_data.header)));
            update_callback<std::function<void(std::wstring)>>(control, new_data.on_changed);
        }
        else if constexpr (std::is_same_v<T, CheckBoxElement>) {
            auto cb = control.as<controls::CheckBox>();
            auto& old_data = std::get<CheckBoxElement>(old_el.data);
            if (old_data.is_checked != new_data.is_checked)
                cb.IsChecked(new_data.is_checked);
            if (old_data.label != new_data.label && new_data.label)
                cb.Content(winrt::box_value(to_hstring(*new_data.label)));
            update_callback<std::function<void(bool)>>(control, new_data.on_changed);
        }
        else if constexpr (std::is_same_v<T, ToggleSwitchElement>) {
            auto ts = control.as<controls::ToggleSwitch>();
            auto& old_data = std::get<ToggleSwitchElement>(old_el.data);
            if (old_data.is_on != new_data.is_on) ts.IsOn(new_data.is_on);
            if (old_data.on_content != new_data.on_content && new_data.on_content)
                ts.OnContent(winrt::box_value(to_hstring(*new_data.on_content)));
            if (old_data.off_content != new_data.off_content && new_data.off_content)
                ts.OffContent(winrt::box_value(to_hstring(*new_data.off_content)));
            update_callback<std::function<void(bool)>>(control, new_data.on_changed);
        }
        else if constexpr (std::is_same_v<T, RadioButtonElement>) {
            auto rb = control.as<controls::RadioButton>();
            auto& old_data = std::get<RadioButtonElement>(old_el.data);
            if (old_data.is_checked != new_data.is_checked) rb.IsChecked(new_data.is_checked);
            update_callback<std::function<void(bool)>>(control, new_data.on_changed);
        }
        else if constexpr (std::is_same_v<T, SliderElement>) {
            auto sl = control.as<controls::Slider>();
            auto& old_data = std::get<SliderElement>(old_el.data);
            if (old_data.min != new_data.min) sl.Minimum(new_data.min);
            if (old_data.max != new_data.max) sl.Maximum(new_data.max);
            if (old_data.value != new_data.value) sl.Value(new_data.value);
            update_callback<std::function<void(double)>>(control, new_data.on_changed);
        }
        else if constexpr (std::is_same_v<T, ProgressElement>) {
            auto pb = control.as<controls::ProgressBar>();
            auto& old_data = std::get<ProgressElement>(old_el.data);
            if (old_data.is_indeterminate != new_data.is_indeterminate)
                pb.IsIndeterminate(new_data.is_indeterminate);
            if (!new_data.is_indeterminate && old_data.value != new_data.value)
                pb.Value(new_data.value);
        }
        else if constexpr (std::is_same_v<T, ComboBoxElement>) {
            auto cb = control.as<controls::ComboBox>();
            auto& old_data = std::get<ComboBoxElement>(old_el.data);
            // Rebuild items if they changed
            if (old_data.items != new_data.items) {
                cb.Items().Clear();
                for (const auto& item : new_data.items) {
                    cb.Items().Append(winrt::box_value(to_hstring(item)));
                }
            }
            if (old_data.selected_index != new_data.selected_index)
                cb.SelectedIndex(new_data.selected_index);
            update_callback<std::function<void(int)>>(control, new_data.on_changed);
        }
        else if constexpr (std::is_same_v<T, StackElement>) {
            auto sp = control.as<controls::StackPanel>();
            auto& old_data = std::get<StackElement>(old_el.data);
            if (old_data.orientation != new_data.orientation) {
                sp.Orientation(new_data.orientation == Orientation::Vertical
                    ? controls::Orientation::Vertical : controls::Orientation::Horizontal);
            }
            if (old_data.spacing != new_data.spacing) sp.Spacing(new_data.spacing);
            reconcile_children(old_data.children, new_data.children, sp, request_rerender);
        }
        else if constexpr (std::is_same_v<T, GridElement>) {
            auto grid = control.as<controls::Grid>();
            auto& old_data = std::get<GridElement>(old_el.data);
            // For simplicity, re-use panel child reconciliation
            // Grid definition changes are expensive, so only rebuild if needed
            if (old_data.definition != new_data.definition) {
                // Full rebuild of definitions
                grid.ColumnDefinitions().Clear();
                grid.RowDefinitions().Clear();
                if (!new_data.definition.columns.empty()) {
                    // Re-parse
                    std::wistringstream iss(new_data.definition.columns);
                    std::wstring token;
                    while (iss >> token) {
                        controls::ColumnDefinition cd;
                        if (token == L"Auto") cd.Width({ 0, xaml::GridUnitType::Auto });
                        else if (token.back() == L'*') {
                            double val = (token.size() == 1) ? 1.0 : std::stod(token.substr(0, token.size()-1));
                            cd.Width({ val, xaml::GridUnitType::Star });
                        } else cd.Width({ std::stod(token), xaml::GridUnitType::Pixel });
                        grid.ColumnDefinitions().Append(cd);
                    }
                }
                if (!new_data.definition.rows.empty()) {
                    std::wistringstream iss(new_data.definition.rows);
                    std::wstring token;
                    while (iss >> token) {
                        controls::RowDefinition rd;
                        if (token == L"Auto") rd.Height({ 0, xaml::GridUnitType::Auto });
                        else if (token.back() == L'*') {
                            double val = (token.size() == 1) ? 1.0 : std::stod(token.substr(0, token.size()-1));
                            rd.Height({ val, xaml::GridUnitType::Star });
                        } else rd.Height({ std::stod(token), xaml::GridUnitType::Pixel });
                        grid.RowDefinitions().Append(rd);
                    }
                }
            }
            reconcile_children(old_data.children, new_data.children, grid, request_rerender);
        }
        else if constexpr (std::is_same_v<T, BorderElement>) {
            auto border = control.as<controls::Border>();
            auto& old_data = std::get<BorderElement>(old_el.data);
            auto existing_child = border.Child();
            auto new_child = reconcile(&*old_data.child, *new_data.child, existing_child, request_rerender);
            if (new_child != existing_child) border.Child(new_child);
        }
        else if constexpr (std::is_same_v<T, ScrollViewElement>) {
            auto sv = control.as<controls::ScrollViewer>();
            auto& old_data = std::get<ScrollViewElement>(old_el.data);
            auto existing_child = sv.Content() ? sv.Content().try_as<xaml::UIElement>() : xaml::UIElement{ nullptr };
            auto new_child = reconcile(&*old_data.child, *new_data.child, existing_child, request_rerender);
            if (new_child != existing_child) sv.Content(new_child);
        }
        else if constexpr (std::is_same_v<T, ImageElement>) {
            auto img = control.as<controls::Image>();
            auto& old_data = std::get<ImageElement>(old_el.data);
            if (old_data.uri != new_data.uri) {
                winrt::Microsoft::UI::Xaml::Media::Imaging::BitmapImage bmp;
                bmp.UriSource(winrt::Windows::Foundation::Uri(to_hstring(new_data.uri)));
                img.Source(bmp);
            }
        }
        else if constexpr (std::is_same_v<T, ComponentElement>) {
            // Reconcile the component
            uint64_t key = get_component_id(control);
            auto it = key ? component_nodes_.find(key) : component_nodes_.end();
            if (it != component_nodes_.end()) {
                auto& node = it->second;
#ifdef DUCT_DEBUG_LOG
                OutputDebugStringA("UPDATE component: re-rendering\n");
#endif
                node.component->begin_render();
                node.component->context().set_request_render(request_rerender);
                auto new_child_element = node.component->render();
                node.component->flush_effects();

                auto border = control.as<controls::Border>();
                auto existing_child = border.Child();
                auto new_child_control = reconcile(&node.rendered_element, new_child_element, existing_child, request_rerender);
                if (new_child_control != existing_child) {
#ifdef DUCT_DEBUG_LOG
                    OutputDebugStringA("UPDATE component: child control REPLACED\n");
#endif
                    border.Child(new_child_control);
                }
#ifdef DUCT_DEBUG_LOG
                else {
                    OutputDebugStringA("UPDATE component: child control unchanged (updated in place)\n");
                }
#endif

                node.rendered_element = std::move(new_child_element);
                node.rendered_control = new_child_control;
            } else {
#ifdef DUCT_DEBUG_LOG
                OutputDebugStringA("UPDATE component: WARNING node not found!\n");
#endif
            }
        }
        else if constexpr (std::is_same_v<T, FuncElement>) {
            uint64_t key = get_component_id(control);
            auto it = key ? component_nodes_.find(key) : component_nodes_.end();
            if (it != component_nodes_.end()) {
                auto& node = it->second;
                node.func_context->begin_render();
                node.func_context->set_request_render(request_rerender);
                auto new_child_element = new_data.render(*node.func_context);
                node.func_context->flush_effects();

                auto border = control.as<controls::Border>();
                auto existing_child = border.Child();
                auto new_child_control = reconcile(&node.rendered_element, new_child_element, existing_child, request_rerender);
                if (new_child_control != existing_child) border.Child(new_child_control);

                node.rendered_element = std::move(new_child_element);
                node.rendered_control = new_child_control;
            }
        }
        else if constexpr (std::is_same_v<T, ListViewElement>) {
            auto lv = control.as<controls::ListView>();
            auto& old_data = std::get<ListViewElement>(old_el.data);
            // Rebuild items if count changed (full rebuild for simplicity)
            if (old_data.items.size() != new_data.items.size()) {
                lv.Items().Clear();
                for (const auto& item : new_data.items) {
                    auto item_control = mount(item, request_rerender);
                    if (item_control) {
                        controls::ListViewItem lvi;
                        lvi.Content(item_control);
                        lv.Items().Append(lvi);
                    }
                }
            } else {
                // Update existing items in place
                for (size_t i = 0; i < new_data.items.size(); ++i) {
                    if (i < old_data.items.size()) {
                        auto lvi = lv.Items().GetAt(static_cast<uint32_t>(i)).as<controls::ListViewItem>();
                        auto existing = lvi.Content().try_as<xaml::UIElement>();
                        auto updated = reconcile(&old_data.items[i], new_data.items[i], existing, request_rerender);
                        if (updated != existing) lvi.Content(updated);
                    }
                }
            }
            if (old_data.selected_index != new_data.selected_index && new_data.selected_index) {
                lv.SelectedIndex(*new_data.selected_index);
            }
            if (new_data.on_selection_changed) {
                update_callback<std::function<void(int)>>(control, new_data.on_selection_changed);
            }
        }
        else if constexpr (std::is_same_v<T, FlyoutButtonElement>) {
            auto btn = control.as<controls::Button>();
            auto& old_data = std::get<FlyoutButtonElement>(old_el.data);
            if (old_data.label != new_data.label)
                btn.Content(winrt::box_value(to_hstring(new_data.label)));
            // Rebuild flyout content from scratch
            if (auto flyout = btn.Flyout().try_as<controls::Flyout>()) {
                if (new_data.flyout_children.size() == 1) {
                    auto child_control = mount(new_data.flyout_children[0], request_rerender);
                    if (child_control) flyout.Content(child_control);
                } else {
                    controls::StackPanel sp;
                    sp.Spacing(8);
                    for (const auto& child : new_data.flyout_children) {
                        auto child_control = mount(child, request_rerender);
                        if (child_control) sp.Children().Append(child_control);
                    }
                    flyout.Content(sp);
                }
            }
        }
        else if constexpr (std::is_same_v<T, MenuFlyoutButtonElement>) {
            auto btn = control.as<controls::Button>();
            auto& old_data = std::get<MenuFlyoutButtonElement>(old_el.data);
            if (old_data.label != new_data.label)
                btn.Content(winrt::box_value(to_hstring(new_data.label)));
            // Rebuild menu items if changed
            if (old_data.items.size() != new_data.items.size()) {
                controls::MenuFlyout mf;
                for (const auto& item : new_data.items) {
                    controls::MenuFlyoutItem mfi;
                    mfi.Text(to_hstring(item.label));
                    auto fn = std::make_shared<std::function<void()>>(item.on_click);
                    mfi.Click([fn](winrt::Windows::Foundation::IInspectable const&,
                                   winrt::Microsoft::UI::Xaml::RoutedEventArgs const&) {
                        if (*fn) (*fn)();
                    });
                    mf.Items().Append(mfi);
                }
                btn.Flyout(mf);
            }
        }
    }, new_el.data);

    // Apply modifier diff
    apply_modifiers_diff(control, old_el.modifiers, new_el.modifiers);

    return nullptr; // null means "keep existing control"
}

} // namespace duct
